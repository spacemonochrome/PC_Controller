// ============================================================
//  ControlUI.cpp  –  ESP32-S3 Touch LCD 5B  |  LVGL v8 + EEZ
//
//  ── Sürekli Veri Paketi (PC → ESP32, ~100ms) ──────────────
//  Format : "SES:PAR:CPU:GPU:CTP:GTP:PIL:K\n"
//  Örnek  : "075:080:045:030:067:055:100:1\n"
//
//  ── ESP32 → PC Mesajları ──────────────────────────────────
//  Slider : "X:YYY\n"   (X: 1=ses, 2=ekran)
//  Buton  : "X:T\n"     (X: 3/4/5)
//  ACK    : "X:T\n"     (PC → ESP32)
//
//  ── Slider Gönderim Stratejisi ────────────────────────────
//  - Sürükleme sırasında en fazla her SLIDER_SEND_INTERVAL_MS
//    'de bir gönderim yapılır (rate limit).
//  - Parmak bırakıldığında (LV_EVENT_RELEASED) her zaman
//    son değer gönderilir (kesin son değer PC'ye ulaşır).
// ============================================================

#include <Arduino.h>
#include <esp_display_panel.hpp>
#include <lvgl.h>
#include "lvgl_v8_port.h"
#include "ui.h"
#include "screens.h"
#include "actions.h"

using namespace esp_panel::drivers;
using namespace esp_panel::board;

// ── UART sabitleri ─────────────────────────────────────────
#define UART_BAUD             115200
#define PKT_LEN               29
#define ACK_TIMEOUT_MS        2000
#define SLIDER_BYPASS_MS      2000
#define SLIDER_SEND_INTERVAL_MS 80   // Sürükleme sırasında max gönderim hızı

// ── TX Kuyruğu ─────────────────────────────────────────────
#define TX_QUEUE_SIZE  8
#define TX_MSG_MAXLEN  8

typedef struct { char data[TX_MSG_MAXLEN]; } TxMsg;

static TxMsg   tx_queue[TX_QUEUE_SIZE];
static uint8_t tx_head  = 0;
static uint8_t tx_tail  = 0;
static uint8_t tx_count = 0;

static bool tx_enqueue(const char *msg)
{
    if (tx_count >= TX_QUEUE_SIZE) return false;
    strncpy(tx_queue[tx_tail].data, msg, TX_MSG_MAXLEN - 1);
    tx_queue[tx_tail].data[TX_MSG_MAXLEN - 1] = '\0';
    tx_tail = (tx_tail + 1) % TX_QUEUE_SIZE;
    tx_count++;
    return true;
}

static void tx_tick()
{
    if (tx_count == 0) return;
    Serial.print(tx_queue[tx_head].data);
    tx_head = (tx_head + 1) % TX_QUEUE_SIZE;
    tx_count--;
}

// ── Slider rate limit & bypass ─────────────────────────────
typedef struct {
    uint32_t bypass_until;    // PC verisini bypass etme süresi
    uint32_t last_sent_ms;    // Son gönderim zamanı
    int      last_sent_val;   // Son gönderilen değer
    bool     dirty;           // Bırakılınca gönderilecek bekleyen değer var mı
    int      pending_val;     // Bırakılınca gönderilecek değer
} SliderState;

static SliderState sliders[3] = {};  // [1]=ses, [2]=ekran (0 kullanılmıyor)

// ── Buton durum makinesi ───────────────────────────────────
typedef enum { BTN_IDLE = 0, BTN_WAIT_ACK } BtnState;

typedef struct {
    uint8_t  channel;
    BtnState state;
    uint32_t sent_at_ms;
} ButtonContext;

static ButtonContext btn_ctx = { 0, BTN_IDLE, 0 };

// ── RX tamponu ─────────────────────────────────────────────
#define RX_BUF_SIZE  (PKT_LEN + 2)
static char    rx_buf[RX_BUF_SIZE];
static uint8_t rx_pos = 0;

// ═══════════════════════════════════════════════════════════
//  YARDIMCI
// ═══════════════════════════════════════════════════════════
static inline int parse3(const char *p)
{
    return (p[0]-'0')*100 + (p[1]-'0')*10 + (p[2]-'0');
}

// ═══════════════════════════════════════════════════════════
//  GÖNDERIM
// ═══════════════════════════════════════════════════════════
static void uart_send_button(uint8_t channel)
{
    char msg[TX_MSG_MAXLEN];
    snprintf(msg, sizeof(msg), "%u:T\n", (unsigned)channel);
    tx_enqueue(msg);
}

static void uart_send_slider_raw(uint8_t channel, int value)
{
    char msg[TX_MSG_MAXLEN];
    snprintf(msg, sizeof(msg), "%u:%03d\n", (unsigned)channel, value);
    tx_enqueue(msg);
}

// Rate-limit kontrolü ile slider gönder
static void uart_send_slider(uint8_t channel, int value, bool force)
{
    if (channel < 1 || channel > 2) return;
    SliderState *s = &sliders[channel];
    uint32_t now = millis();

    if (force || (now - s->last_sent_ms) >= SLIDER_SEND_INTERVAL_MS) {
        // Gönder
        uart_send_slider_raw(channel, value);
        s->last_sent_ms  = now;
        s->last_sent_val = value;
        s->dirty         = false;
    } else {
        // Henüz erken — bekleyen olarak işaretle, bırakınca gönderilecek
        s->dirty       = true;
        s->pending_val = value;
    }
}

// ═══════════════════════════════════════════════════════════
//  VERİ PAKETİ İŞLE
// ═══════════════════════════════════════════════════════════
static void process_packet(const char *p)
{
    if (p[3]  != ':' || p[7]  != ':' || p[11] != ':' ||
        p[15] != ':' || p[19] != ':' || p[23] != ':' || p[27] != ':') return;

    int ses = parse3(p+0);
    int par = parse3(p+4);
    int cpu = parse3(p+8);
    int gpu = parse3(p+12);
    int ctp = parse3(p+16);
    int gtp = parse3(p+20);
    int pil = parse3(p+24);
    int kay = p[28] - '0';

    if (ses<0||ses>100||par<0||par>100||cpu<0||cpu>100||
        gpu<0||gpu>100||pil<0||pil>100||kay<0||kay>3) return;

    if (!lvgl_port_lock(50)) return;

    uint32_t now = millis();
    char buf[16];

    if (now >= sliders[1].bypass_until) {
        lv_slider_set_value(objects.ses_slider, ses, LV_ANIM_OFF);
        snprintf(buf, sizeof(buf), "%%%d", ses);
        lv_label_set_text(objects.ses_value, buf);
    }

    if (now >= sliders[2].bypass_until) {
        lv_slider_set_value(objects.ekran_slider, par, LV_ANIM_OFF);
        snprintf(buf, sizeof(buf), "%%%d", par);
        lv_label_set_text(objects.ekran_value, buf);
    }

    lv_meter_set_indicator_value(objects.cpu_usage_value,
                                 screen_main_state.indicator, cpu);
    lv_meter_set_indicator_value(objects.gpu_usage_value,
                                 screen_main_state.indicator1, gpu);

    snprintf(buf, sizeof(buf), "CPU Temp %d\xC2\xB0""C", ctp);
    lv_label_set_text(objects.obj5, buf);

    snprintf(buf, sizeof(buf), "GPU Temp %d\xC2\xB0""C", gtp);
    lv_label_set_text(objects.obj6, buf);

    lv_bar_set_value(objects.pil_progressbar, pil, LV_ANIM_OFF);
    snprintf(buf, sizeof(buf), "%%%d", pil);
    lv_label_set_text(objects.pil_value, buf);

    switch (kay) {
        case 0:
            lv_obj_set_style_bg_color(objects.start_button,
                lv_color_hex(0xff008620), LV_PART_MAIN|LV_STATE_DEFAULT);
            lv_obj_set_style_bg_color(objects.durate_button,
                lv_color_hex(0xff787878), LV_PART_MAIN|LV_STATE_DEFAULT);
            lv_obj_set_style_bg_color(objects.stop_button,
                lv_color_hex(0xff787878), LV_PART_MAIN|LV_STATE_DEFAULT);
            if (btn_ctx.state == BTN_IDLE) lv_led_off(objects.led);
            break;
        case 1:
            lv_obj_set_style_bg_color(objects.start_button,
                lv_color_hex(0xff008620), LV_PART_MAIN|LV_STATE_DEFAULT);
            lv_obj_set_style_bg_color(objects.durate_button,
                lv_color_hex(0xffca7800), LV_PART_MAIN|LV_STATE_DEFAULT);
            lv_obj_set_style_bg_color(objects.stop_button,
                lv_color_hex(0xffcc0000), LV_PART_MAIN|LV_STATE_DEFAULT);
            if (btn_ctx.state == BTN_IDLE) {
                lv_led_on(objects.led);
                lv_led_set_color(objects.led, lv_color_hex(0xffcc0000));
            }
            break;
        case 2:
            lv_obj_set_style_bg_color(objects.durate_button,
                lv_color_hex(0xffca7800), LV_PART_MAIN|LV_STATE_DEFAULT);
            if (btn_ctx.state == BTN_IDLE) {
                lv_led_on(objects.led);
                lv_led_set_color(objects.led, lv_color_hex(0xffffaa00));
            }
            break;
        case 3:
            lv_obj_set_style_bg_color(objects.start_button,
                lv_color_hex(0xff008620), LV_PART_MAIN|LV_STATE_DEFAULT);
            lv_obj_set_style_bg_color(objects.durate_button,
                lv_color_hex(0xff787878), LV_PART_MAIN|LV_STATE_DEFAULT);
            lv_obj_set_style_bg_color(objects.stop_button,
                lv_color_hex(0xff787878), LV_PART_MAIN|LV_STATE_DEFAULT);
            if (btn_ctx.state == BTN_IDLE) lv_led_off(objects.led);
            break;
    }

    lvgl_port_unlock();
}

// ═══════════════════════════════════════════════════════════
//  ACK İŞLE
// ═══════════════════════════════════════════════════════════
static void process_ack(const char *msg)
{
    uint8_t ch = (uint8_t)(msg[0] - '0');
    if (btn_ctx.state == BTN_WAIT_ACK && btn_ctx.channel == ch) {
        if (lvgl_port_lock(50)) {
            lv_led_on(objects.led);
            lv_led_set_color(objects.led, lv_color_hex(0xff00cc44));
            lvgl_port_unlock();
        }
        btn_ctx.state = BTN_IDLE;
    }
}

// ═══════════════════════════════════════════════════════════
//  UART OKUMA
// ═══════════════════════════════════════════════════════════
static void uart_tick()
{
    while (Serial.available() > 0) {
        char c = (char)Serial.read();

        if (rx_pos < (uint8_t)(RX_BUF_SIZE - 1))
            rx_buf[rx_pos++] = c;

        if (c == '\n') {
            rx_buf[rx_pos] = '\0';
            if      (rx_pos == PKT_LEN)                    process_packet(rx_buf);
            else if (rx_pos >= 4 && rx_buf[2] == 'T')      process_ack(rx_buf);
            rx_pos = 0;
            continue;
        }

        if (rx_pos >= (uint8_t)(RX_BUF_SIZE - 1)) rx_pos = 0;
    }
}

// ═══════════════════════════════════════════════════════════
//  ACK TIMEOUT
// ═══════════════════════════════════════════════════════════
static void ack_timeout_tick()
{
    if (btn_ctx.state != BTN_WAIT_ACK) return;
    if ((millis() - btn_ctx.sent_at_ms) >= ACK_TIMEOUT_MS) {
        if (lvgl_port_lock(50)) {
            lv_led_on(objects.led);
            lv_led_set_color(objects.led, lv_color_hex(0xffcc0000));
            lvgl_port_unlock();
        }
        btn_ctx.state = BTN_IDLE;
    }
}

// ═══════════════════════════════════════════════════════════
//  ACTION HANDLER
// ═══════════════════════════════════════════════════════════
extern "C" void action_eylemsel(lv_event_t *e)
{
    uint8_t channel = (uint8_t)(uintptr_t)lv_event_get_user_data(e);
    lv_event_code_t code = lv_event_get_code(e);

    // ── Slider sürükleniyor ───────────────────────────────
    if (code == LV_EVENT_VALUE_CHANGED) {
        if (channel < 1 || channel > 2) return;

        lv_obj_t *slider = lv_event_get_target(e);
        int val = lv_slider_get_value(slider);

        // Label güncelle
        char buf[8];
        snprintf(buf, sizeof(buf), "%%%d", val);
        if (channel == 1) lv_label_set_text(objects.ses_value,   buf);
        else              lv_label_set_text(objects.ekran_value,  buf);

        // Bypass süresini uzat
        sliders[channel].bypass_until = millis() + SLIDER_BYPASS_MS;

        // Rate-limit ile gönder
        uart_send_slider(channel, val, false);
        return;
    }

    // ── Parmak bırakıldı → son değeri kesin gönder ────────
    if (code == LV_EVENT_RELEASED) {
        if (channel < 1 || channel > 2) return;

        SliderState *s = &sliders[channel];
        if (s->dirty) {
            uart_send_slider(channel, s->pending_val, true);  // force=true
        }
        return;
    }

    // ── Buton basımı ──────────────────────────────────────
    if (code == LV_EVENT_PRESSED) {
        if (btn_ctx.state == BTN_WAIT_ACK) return;

        lv_led_on(objects.led);
        lv_led_set_color(objects.led, lv_color_hex(0xffffaa00));

        uart_send_button(channel);

        btn_ctx.channel    = channel;
        btn_ctx.state      = BTN_WAIT_ACK;
        btn_ctx.sent_at_ms = millis();
    }
}

// ═══════════════════════════════════════════════════════════
//  SETUP
// ═══════════════════════════════════════════════════════════
void setup()
{
    Serial.begin(UART_BAUD);

    Serial.println("Initializing board");
    Board *board = new Board();
    board->init();

#if LVGL_PORT_AVOID_TEARING_MODE
    auto lcd = board->getLCD();
    lcd->configFrameBufferNumber(LVGL_PORT_DISP_BUFFER_NUM);
#if ESP_PANEL_DRIVERS_BUS_ENABLE_RGB && CONFIG_IDF_TARGET_ESP32S3
    auto lcd_bus = lcd->getBus();
    if (lcd_bus->getBasicAttributes().type == ESP_PANEL_BUS_TYPE_RGB) {
        static_cast<BusRGB *>(lcd_bus)->configRGB_BounceBufferSize(lcd->getFrameWidth() * 10);
    }
#endif
#endif

    assert(board->begin());

    Serial.println("Initializing LVGL");
    lvgl_port_init(board->getLCD(), board->getTouch());

    Serial.println("Creating UI");
    lvgl_port_lock(-1);

    ui_init();
    lv_led_off(objects.led);

    lvgl_port_unlock();
}

// ═══════════════════════════════════════════════════════════
//  LOOP
// ═══════════════════════════════════════════════════════════
void loop()
{
    uart_tick();
    tx_tick();
    ack_timeout_tick();

    if (lvgl_port_lock(5)) {
        ui_tick();
        lvgl_port_unlock();
    }

    delay(5);
}
