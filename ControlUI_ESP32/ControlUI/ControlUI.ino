// ============================================================
<<<<<<< Updated upstream
//  ControlUI.cpp  –  ESP32-S3 Touch LCD 5B  |  LVGL v8 + EEZ
//
//  PC → ESP32 : "SES:PAR:CPU:GPU:CTP:GTP:PIL:K\n"  (29 byte)
//  ESP32 → PC : "X:YYY\n"  slider  (X=1/2, YYY=000-100)
//  ESP32 → PC : "X:T\n"    buton   (X=3/4/5)
//  PC → ESP32 : "X:T\n"    ACK     (X=3/4/5)
=======
//  ControlUI.ino  –  ESP32-S3 Touch LCD 5B  |  LVGL v8 + EEZ
//
//  PC → ESP32 : "SES:PAR:CPU:GPU:CTP:GTP:PIL:K\n"
//               K = 0:idle  1:kayıt  2:duraklatma  3:durduruldu
//
//  ESP32 → PC : "X:YYY\n"  (slider, ch=1 ses / ch=2 ekran)
//               "X:T\n"    (OBS buton, ch=3 start / ch=4 pause / ch=5 stop)
>>>>>>> Stashed changes
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

// ── Sabitler ──────────────────────────────────────────────
#define UART_BAUD               115200
#define PKT_LEN                 29        // "SES:PAR:CPU:GPU:CTP:GTP:PIL:K\n"
#define ACK_TIMEOUT_MS          2000
#define SLIDER_BYPASS_MS        2000      // Dokunuştan sonra PC verisini reddet
#define SLIDER_SEND_INTERVAL_MS 80        // Max slider gönderim hızı

// ── TX Kuyruğu ────────────────────────────────────────────
#define TX_QUEUE_SIZE  8
#define TX_MSG_MAXLEN  10

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

// ── Slider durumu (ESP32 ekran tarafı) ────────────────────
// bypass_until : Bu zamana kadar PC'den gelen bu kanalın
//                verisini UI'a yansıtma (kullanıcı dokundu)
// last_sent_ms : Rate limiting için son gönderim zamanı
// dirty        : Bırakılınca gönderilecek bekleyen değer var mı
// pending_val  : Bekleyen değer
typedef struct {
    uint32_t bypass_until;
    uint32_t last_sent_ms;
    bool     dirty;
    int      pending_val;
} SliderState;

static SliderState sliders[3] = {};  // index 1=ses, 2=ekran

<<<<<<< Updated upstream
// ── Buton ACK durumu ──────────────────────────────────────
=======
// ── Buton ACK Durumu ──────────────────────────────────────
// OBS butonları (ch=3,4,5) için ayrı bir ACK bekleme mekanizması
// yoktur; PC doğrudan pakette K alanını günceller.
// Slider butonları (ch=1,2) için ACK bekleme korunur.
>>>>>>> Stashed changes
typedef enum { BTN_IDLE = 0, BTN_WAIT_ACK } BtnState;

static struct {
    uint8_t  channel;
    BtnState state;
    uint32_t sent_at_ms;
} btn_ctx = { 0, BTN_IDLE, 0 };

<<<<<<< Updated upstream
// ── RX tamponu ────────────────────────────────────────────
=======
// ── Lokal OBS durumu (son gelen K değeri) ─────────────────
// 0=idle 1=kayıt 2=duraklatma 3=durduruldu
static int obs_state = 0;

// ── PC bağlantı durumu ────────────────────────────────────
// İlk geçerli paket alındığında true olur
static bool pc_connected = false;

// ── RX Tamponu ────────────────────────────────────────────
>>>>>>> Stashed changes
#define RX_BUF_SIZE  (PKT_LEN + 4)
static char    rx_buf[RX_BUF_SIZE];
static uint8_t rx_pos = 0;

// ═══════════════════════════════════════════════════════════
//  YARDIMCI
// ═══════════════════════════════════════════════════════════
static inline int parse3(const char *p)
{
    return (p[0]-'0')*100 + (p[1]-'0')*10 + (p[2]-'0');
}

static inline int clamp100(int v)
{
    return v < 0 ? 0 : v > 100 ? 100 : v;
}

// ═══════════════════════════════════════════════════════════
//  UART GÖNDER
// ═══════════════════════════════════════════════════════════
static void uart_send_button(uint8_t ch)
{
    char msg[TX_MSG_MAXLEN];
    snprintf(msg, sizeof(msg), "%u:T\n", (unsigned)ch);
    tx_enqueue(msg);
}

static void uart_send_slider(uint8_t ch, int val, bool force)
{
    if (ch < 1 || ch > 2) return;
    SliderState *s = &sliders[ch];
    uint32_t now = millis();

    if (force || (now - s->last_sent_ms) >= SLIDER_SEND_INTERVAL_MS) {
        char msg[TX_MSG_MAXLEN];
        snprintf(msg, sizeof(msg), "%u:%03d\n", (unsigned)ch, val);
        tx_enqueue(msg);
        s->last_sent_ms = now;
        s->dirty        = false;
    } else {
        s->dirty       = true;
        s->pending_val = val;
    }
}

// ═══════════════════════════════════════════════════════════
<<<<<<< Updated upstream
//  GELEN PC PAKETİNİ İŞLE
//  "SES:PAR:CPU:GPU:CTP:GTP:PIL:K\n"
//   [0] [4] [8] [12][16][20][24][28]
=======
//  OBS UI GÜNCELLEMESİ
//  PC'den gelen K değerine veya lokal duruma göre
//  buton renklerini ve LED'i günceller
// ═══════════════════════════════════════════════════════════
static void apply_obs_ui(int state)
{
    obs_state = state;

    // OBS buton etiket objelerini bul (lv_obj_get_child ile ilk çocuk)
    lv_obj_t *start_label  = lv_obj_get_child(objects.start_button,  0);
    lv_obj_t *durate_label = lv_obj_get_child(objects.durate_button, 0);

    switch (state) {
        case 0: // idle / bağlı değil
        case 3: // durduruldu
            lv_obj_set_style_bg_color(objects.start_button,
                lv_color_hex(0xff008620), LV_PART_MAIN | LV_STATE_DEFAULT);
            lv_obj_set_style_opa(objects.start_button, LV_OPA_COVER, LV_PART_MAIN | LV_STATE_DEFAULT);
            lv_obj_set_style_bg_color(objects.durate_button,
                lv_color_hex(0xff787878), LV_PART_MAIN | LV_STATE_DEFAULT);
            lv_obj_set_style_bg_color(objects.stop_button,
                lv_color_hex(0xff787878), LV_PART_MAIN | LV_STATE_DEFAULT);
            // Duraklat butonunu "b" (pause ikonu) olarak sıfırla
            if (durate_label) lv_label_set_text(durate_label, "b");
            lv_led_off(objects.led);
            break;

        case 1: // kayıt
            lv_obj_set_style_bg_color(objects.start_button,
                lv_color_hex(0xff444444), LV_PART_MAIN | LV_STATE_DEFAULT);
            lv_obj_set_style_opa(objects.start_button, LV_OPA_40, LV_PART_MAIN | LV_STATE_DEFAULT);
            lv_obj_set_style_bg_color(objects.durate_button,
                lv_color_hex(0xffca7800), LV_PART_MAIN | LV_STATE_DEFAULT);
            lv_obj_set_style_bg_color(objects.stop_button,
                lv_color_hex(0xffcc0000), LV_PART_MAIN | LV_STATE_DEFAULT);
            if (durate_label) lv_label_set_text(durate_label, "b");
            lv_led_on(objects.led);
            lv_led_set_color(objects.led, lv_color_hex(0xffcc0000));
            break;

        case 2: // duraklatma
            lv_obj_set_style_bg_color(objects.start_button,
                lv_color_hex(0xff444444), LV_PART_MAIN | LV_STATE_DEFAULT);
            lv_obj_set_style_opa(objects.start_button, LV_OPA_40, LV_PART_MAIN | LV_STATE_DEFAULT);
            lv_obj_set_style_bg_color(objects.durate_button,
                lv_color_hex(0xffca7800), LV_PART_MAIN | LV_STATE_DEFAULT);
            lv_obj_set_style_bg_color(objects.stop_button,
                lv_color_hex(0xffcc0000), LV_PART_MAIN | LV_STATE_DEFAULT);
            // Duraklat butonunu "devam et" ikonu olarak değiştir (play: "a")
            if (durate_label) lv_label_set_text(durate_label, "a");
            lv_led_on(objects.led);
            lv_led_set_color(objects.led, lv_color_hex(0xffffaa00));
            break;
    }
}

// ═══════════════════════════════════════════════════════════
//  PC PAKETİ İŞLEME
>>>>>>> Stashed changes
// ═══════════════════════════════════════════════════════════
static void process_packet(const char *p, int len)
{
    // Uzunluk kontrolü
    if (len < 29) return;

    // Ayırıcı kontrolü
    if (p[3]  != ':' || p[7]  != ':' || p[11] != ':' ||
        p[15] != ':' || p[19] != ':' || p[23] != ':' || p[27] != ':') return;

    int ses = parse3(p + 0);
    int par = parse3(p + 4);
    int cpu = parse3(p + 8);
    int gpu = parse3(p + 12);
    int ctp = parse3(p + 16);
    int gtp = parse3(p + 20);
    int pil = parse3(p + 24);
    int kay = p[28] - '0';  // OBS durumu: 0,1,2,3

<<<<<<< Updated upstream
    // Sınır kontrolü
    if (ses < 0 || ses > 100) return;
    if (par < 0 || par > 100) return;
    if (cpu < 0 || cpu > 100) return;
    if (gpu < 0 || gpu > 100) return;
    if (pil < 0 || pil > 100) return;
    if (kay < 0 || kay > 3)   return;
=======
    if (ses < 0 || ses > 100 || par < 0 || par > 100 ||
        cpu < 0 || cpu > 100 || gpu < 0 || gpu > 100 ||
        pil < 0 || pil > 100 || kay < 0 || kay > 3) return;
>>>>>>> Stashed changes

    if (!lvgl_port_lock(50)) return;

    pc_connected = true;

    uint32_t now = millis();
    char buf[20];

<<<<<<< Updated upstream
    // ── Ses slider ────────────────────────────────────────
=======
    // ── Slider'lar ──────────────────────────────────────
>>>>>>> Stashed changes
    if (now >= sliders[1].bypass_until) {
        lv_slider_set_value(objects.ses_slider, ses, LV_ANIM_ON);
        snprintf(buf, sizeof(buf), "%%%d", ses);
        lv_label_set_text(objects.ses_value, buf);
    }

    // ── Ekran parlaklık slider ────────────────────────────
    if (now >= sliders[2].bypass_until) {
        lv_slider_set_value(objects.ekran_slider, par, LV_ANIM_ON);
        snprintf(buf, sizeof(buf), "%%%d", par);
        lv_label_set_text(objects.ekran_value, buf);
    }

<<<<<<< Updated upstream
    // ── CPU metre ─────────────────────────────────────────
    lv_meter_set_indicator_value(objects.cpu_usage_value,
                                 screen_main_state.indicator, cpu);

    // ── GPU metre ─────────────────────────────────────────
    lv_meter_set_indicator_value(objects.gpu_usage_value,
                                 screen_main_state.indicator1, gpu);

    // ── CPU sıcaklık ──────────────────────────────────────
    snprintf(buf, sizeof(buf), "CPU Temp %d\xC2\xB0""C", ctp);
    lv_label_set_text(objects.obj5, buf);

    // ── GPU sıcaklık ──────────────────────────────────────
    snprintf(buf, sizeof(buf), "GPU Temp %d\xC2\xB0""C", gtp);
    lv_label_set_text(objects.obj6, buf);

    // ── Pil progress bar ──────────────────────────────────
=======
    // ── CPU / GPU Gösterge ───────────────────────────────
    lv_meter_set_indicator_value(objects.cpu_usage_value, screen_main_state.indicator,  cpu);
    lv_meter_set_indicator_value(objects.gpu_usage_value, screen_main_state.indicator1, gpu);

    // ── Sıcaklık etiketleri ─────────────────────────────
    snprintf(buf, sizeof(buf), "CPU Temp %d\xc2\xb0""C", ctp);
    lv_label_set_text(objects.obj5, buf);

    snprintf(buf, sizeof(buf), "GPU Temp %d\xc2\xb0""C", gtp);
    lv_label_set_text(objects.obj6, buf);

    // ── Sıcaklık grafiği (80 ms throttle) ───────────────
    static uint32_t last_chart_update = 0;
    if (now - last_chart_update >= 80) {
        lv_chart_set_next_value(objects.temp_chart, screen_main_state.cpu_temp_series, ctp);
        lv_chart_set_next_value(objects.temp_chart, screen_main_state.gpu_temp_series, gtp);
        last_chart_update = now;
    }

    // ── Pil ─────────────────────────────────────────────
>>>>>>> Stashed changes
    lv_bar_set_value(objects.pil_progressbar, pil, LV_ANIM_ON);
    snprintf(buf, sizeof(buf), "%%%d", pil);
    lv_label_set_text(objects.pil_value, buf);

<<<<<<< Updated upstream
    // ── Kayıt durumu ──────────────────────────────────────
    switch (kay) {
        case 0: // Boşta
            lv_obj_set_style_bg_color(objects.start_button,
                lv_color_hex(0xff008620), LV_PART_MAIN | LV_STATE_DEFAULT);
            lv_obj_set_style_bg_color(objects.durate_button,
                lv_color_hex(0xff787878), LV_PART_MAIN | LV_STATE_DEFAULT);
            lv_obj_set_style_bg_color(objects.stop_button,
                lv_color_hex(0xff787878), LV_PART_MAIN | LV_STATE_DEFAULT);
            if (btn_ctx.state == BTN_IDLE) lv_led_off(objects.led);
            break;
        case 1: // Kayıtta
            lv_obj_set_style_bg_color(objects.start_button,
                lv_color_hex(0xff008620), LV_PART_MAIN | LV_STATE_DEFAULT);
            lv_obj_set_style_bg_color(objects.durate_button,
                lv_color_hex(0xffca7800), LV_PART_MAIN | LV_STATE_DEFAULT);
            lv_obj_set_style_bg_color(objects.stop_button,
                lv_color_hex(0xffcc0000), LV_PART_MAIN | LV_STATE_DEFAULT);
            if (btn_ctx.state == BTN_IDLE) {
                lv_led_on(objects.led);
                lv_led_set_color(objects.led, lv_color_hex(0xffcc0000));
            }
            break;
        case 2: // Duraklatmada
            lv_obj_set_style_bg_color(objects.durate_button,
                lv_color_hex(0xffca7800), LV_PART_MAIN | LV_STATE_DEFAULT);
            if (btn_ctx.state == BTN_IDLE) {
                lv_led_on(objects.led);
                lv_led_set_color(objects.led, lv_color_hex(0xffffaa00));
            }
            break;
        case 3: // Durduruldu
            lv_obj_set_style_bg_color(objects.start_button,
                lv_color_hex(0xff008620), LV_PART_MAIN | LV_STATE_DEFAULT);
            lv_obj_set_style_bg_color(objects.durate_button,
                lv_color_hex(0xff787878), LV_PART_MAIN | LV_STATE_DEFAULT);
            lv_obj_set_style_bg_color(objects.stop_button,
                lv_color_hex(0xff787878), LV_PART_MAIN | LV_STATE_DEFAULT);
            if (btn_ctx.state == BTN_IDLE) lv_led_off(objects.led);
            break;
=======
    // ── OBS durumu (K alanı) ────────────────────────────
    // Sadece durum değiştiyse güncelle (gereksiz yeniden çizimi önler)
    if (kay != obs_state) {
        apply_obs_ui(kay);
>>>>>>> Stashed changes
    }

    lvgl_port_unlock();
}

// ═══════════════════════════════════════════════════════════
<<<<<<< Updated upstream
//  GELEN ACK'İ İŞLE  "X:T"
=======
//  ACK İŞLEME (sadece slider ch=1,2 için)
>>>>>>> Stashed changes
// ═══════════════════════════════════════════════════════════
static void process_ack(uint8_t ch)
{
    if (btn_ctx.state != BTN_WAIT_ACK || btn_ctx.channel != ch) return;

    if (lvgl_port_lock(50)) {
        lv_led_on(objects.led);
        lv_led_set_color(objects.led, lv_color_hex(0xff00cc44));
        lvgl_port_unlock();
    }
    btn_ctx.state = BTN_IDLE;
}

// ═══════════════════════════════════════════════════════════
//  UART OKUMA DÖNGÜSÜ
// ═══════════════════════════════════════════════════════════
static void uart_tick()
{
    while (Serial.available() > 0) {
        char c = (char)Serial.read();

        if (rx_pos < (uint8_t)(RX_BUF_SIZE - 1))
            rx_buf[rx_pos++] = c;

        if (c == '\n') {
            rx_buf[rx_pos] = '\0';
            int len = (int)rx_pos;
            rx_pos = 0;

<<<<<<< Updated upstream
            if (len < 4) continue;

            // ':' sayısına göre mesaj tipini belirle
            int colon_count = 0;
            for (int i = 0; i < len; i++)
                if (rx_buf[i] == ':') colon_count++;

            if (colon_count == 7) {
                // Ana 7li veri paketi
                process_packet(rx_buf, len);
            }
            else if (colon_count == 1 && len >= 4) {
                uint8_t ch = (uint8_t)(rx_buf[0] - '0');
                if (ch < 1 || ch > 5) continue;

                if (rx_buf[2] == 'T') {
                    // ACK: "X:T\n"
                    process_ack(ch);
                }
                else if (ch <= 2 && len >= 6) {
                    // Slider: "X:YYY\n" — PC'den gelmiyor,
                    // bu mesaj ESP32'den PC'ye gidiyor.
                    // Buraya düşmemeli ama gelse de ignore.
=======
            if (len >= 4) {
                int colon_count = 0;
                for (int i = 0; i < len; i++)
                    if (rx_buf[i] == ':') colon_count++;

                if (colon_count == 7) {
                    // PC sistem paketi
                    process_packet(rx_buf, len);
                }
                else if (colon_count == 1 && len >= 4) {
                    // ACK: "X:T\n"  (yalnızca slider ch=1,2)
                    uint8_t ch = (uint8_t)(rx_buf[0] - '0');
                    if (ch >= 1 && ch <= 2 && rx_buf[2] == 'T') {
                        process_ack(ch);
                    }
>>>>>>> Stashed changes
                }
            }
            continue;
        }

        // Taşma koruması
        if (rx_pos >= (uint8_t)(RX_BUF_SIZE - 1))
            rx_pos = 0;
    }
}

// ═══════════════════════════════════════════════════════════
//  ACK TIMEOUT (slider butonları için)
// ═══════════════════════════════════════════════════════════
static void ack_timeout_tick()
{
    if (btn_ctx.state != BTN_WAIT_ACK) return;
    if ((millis() - btn_ctx.sent_at_ms) < ACK_TIMEOUT_MS) return;

    if (lvgl_port_lock(50)) {
        lv_led_on(objects.led);
        lv_led_set_color(objects.led, lv_color_hex(0xffcc0000));
        lvgl_port_unlock();
    }
    btn_ctx.state = BTN_IDLE;
}

// ═══════════════════════════════════════════════════════════
<<<<<<< Updated upstream
//  ACTION HANDLER (LVGL event callback)
=======
//  LVGL EVENT HANDLER
//
//  Slider (ch=1,2) : VALUE_CHANGED, RELEASED
//  OBS butonları   : PRESSED  ch=3(start) ch=4(pause) ch=5(stop)
>>>>>>> Stashed changes
// ═══════════════════════════════════════════════════════════
extern "C" void action_eylemsel(lv_event_t *e)
{
    uint8_t channel = (uint8_t)(uintptr_t)lv_event_get_user_data(e);
    lv_event_code_t code = lv_event_get_code(e);

<<<<<<< Updated upstream
    // ── Slider sürükleniyor ───────────────────────────────
=======
    static uint32_t last_label_update[3] = {0};

    // ── Slider: sürekli değişim ──────────────────────────
>>>>>>> Stashed changes
    if (code == LV_EVENT_VALUE_CHANGED) {
        if (channel < 1 || channel > 2) return;

        lv_obj_t *slider = lv_event_get_target(e);
        int val = lv_slider_get_value(slider);

<<<<<<< Updated upstream
        // Label güncelle
        char buf[8];
        snprintf(buf, sizeof(buf), "%%%d", val);
        if (channel == 1) lv_label_set_text(objects.ses_value,  buf);
        else              lv_label_set_text(objects.ekran_value, buf);
=======
        uint32_t now = millis();
        if (now - last_label_update[channel] >= 50) {
            char buf[8];
            snprintf(buf, sizeof(buf), "%%%d", val);
            if (channel == 1)
                lv_label_set_text(objects.ses_value, buf);
            else
                lv_label_set_text(objects.ekran_value, buf);
            last_label_update[channel] = now;
        }
>>>>>>> Stashed changes

        // Bypass süresini uzat — PC gönderdiğinde üstüne yazmasın
        sliders[channel].bypass_until = millis() + SLIDER_BYPASS_MS;

        // Rate-limit ile PC'ye gönder
        uart_send_slider(channel, val, false);
        return;
    }

<<<<<<< Updated upstream
    // ── Parmak bırakıldı → son değeri mutlaka gönder ──────
=======
    // ── Slider: bırakıldı ───────────────────────────────
>>>>>>> Stashed changes
    if (code == LV_EVENT_RELEASED) {
        if (channel < 1 || channel > 2) return;
        SliderState *s = &sliders[channel];
        if (s->dirty)
            uart_send_slider(channel, s->pending_val, true);
        return;
    }

<<<<<<< Updated upstream
    // ── Buton basımı ──────────────────────────────────────
    if (code == LV_EVENT_PRESSED) {
        if (btn_ctx.state == BTN_WAIT_ACK) return;  // Zaten bekliyor
=======
    // ── Buton: basıldı ──────────────────────────────────
    if (code == LV_EVENT_PRESSED) {

        // ── OBS butonları (ch=3,4,5) ──────────────────
        if (channel >= 3 && channel <= 5) {
            // Kayıt yokken start dışındaki butonlara basılmasını engelle
            if (channel == 4 && obs_state != 1 && obs_state != 2) return;
            if (channel == 5 && obs_state != 1 && obs_state != 2) return;

            // Komutu PC'ye gönder
            uart_send_button(channel);

            // LED'i yalnızca PC bağlıysa (paket alınmışsa) sarı yak.
            // obs_state == 0 ve hiç paket gelmemişse LED'e dokunma.
            // Basit kriter: son geçerli paket alındıysa obs_state >= 0 zaten set edilmiş olur.
            // PC bağlı değilken obs_state başlangıçta 0'dır ve start_button'a basılabilir;
            // bu durumda LED yakılmaz, sadece komut kuyruğa alınır.
            if (pc_connected && (obs_state > 0 || channel == 3)) {
                lv_led_on(objects.led);
                lv_led_set_color(objects.led, lv_color_hex(0xffffaa00));
            }
            return;
        }

        // ── Slider butonları (ch=1,2) — eski ACK mantığı ──
        if (btn_ctx.state == BTN_WAIT_ACK) return;
>>>>>>> Stashed changes

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

    //Serial.println("Initializing board");
    Board *board = new Board();
    board->init();

#if LVGL_PORT_AVOID_TEARING_MODE
    auto lcd = board->getLCD();
    lcd->configFrameBufferNumber(LVGL_PORT_DISP_BUFFER_NUM);
#if ESP_PANEL_DRIVERS_BUS_ENABLE_RGB && CONFIG_IDF_TARGET_ESP32S3
    auto lcd_bus = lcd->getBus();
    if (lcd_bus->getBasicAttributes().type == ESP_PANEL_BUS_TYPE_RGB) {
        static_cast<BusRGB *>(lcd_bus)->configRGB_BounceBufferSize(
            lcd->getFrameWidth() * 10);
    }
#endif
#endif

    assert(board->begin());

    //Serial.println("Initializing LVGL");
    lvgl_port_init(board->getLCD(), board->getTouch());

    //Serial.println("Creating UI");
    lvgl_port_lock(-1);
    ui_init();
    lv_led_off(objects.led);
    apply_obs_ui(0); // başlangıçta idle durumu
    lvgl_port_unlock();
}

// ═══════════════════════════════════════════════════════════
//  LOOP
// ═══════════════════════════════════════════════════════════
void loop()
{
    uart_tick();        // Gelen veriyi oku ve işle
    tx_tick();          // Kuyruktan 1 mesaj gönder
    ack_timeout_tick(); // ACK zaman aşımı kontrolü

    if (lvgl_port_lock(5)) {
        ui_tick();
        lvgl_port_unlock();
    }

    delay(5);
}
