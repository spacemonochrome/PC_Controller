#include <Arduino.h>
#include <esp_display_panel.hpp>

#include <lvgl.h>
#include "lvgl_v8_port.h"

//#include "libssh_esp32.h"
//#include <WiFi.h>

#include "ui.h"

using namespace esp_panel::drivers;
using namespace esp_panel::board;

const char* ssid = "RealMardin_2.4GHz";
const char* password = "mardin47mardin";

extern "C" void action_eylem1(lv_event_t *e)
{
    lv_obj_t *target = lv_event_get_target(e);
    lv_event_code_t code = lv_event_get_code(e);

    // TurboFAN switch
    if (target == objects.turbo_value && code == LV_EVENT_PRESSED)
    {
        bool turbo_on = lv_obj_has_state(target, LV_STATE_CHECKED);
        Serial.print("TurboFAN: ");
        Serial.println(turbo_on ? "ON" : "OFF");
        // TODO: host PC'ye komut gönder
        return;
    }

    // Sliderlar
    if (code == LV_EVENT_VALUE_CHANGED)
    {
        int32_t val = lv_slider_get_value(target);
        char buf[16];
        snprintf(buf, sizeof(buf), "%%%ld", (long)val);

        intptr_t slider_id = (intptr_t)lv_event_get_user_data(e);

        if (slider_id == 1)
        {
            lv_label_set_text(objects.ses_value, buf);
            Serial.print("Ses: ");
            Serial.println(val);
        }
        else if (slider_id == 2)
        {
            lv_label_set_text(objects.ekran_value, buf);
            Serial.print("Parlaklik: ");
            Serial.println(val);
        }
    }
}

void setup()
{
    Serial.begin(115200);
    Serial.println("=== LVGL 8.4 - 1024x600 Kontrol Paneli Basliyor ===");
    /*
    WiFi.begin(ssid, password);
    while (WiFi.status() != WL_CONNECTED)
    {
        delay(500);
        Serial.print(".");
    }
    Serial.println("\nWiFi bağlı! IP: " + WiFi.localIP().toString());
    */

    Serial.println("Board baslatiliyor...");
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

    Serial.println("LVGL port baslatiliyor...");
    lvgl_port_init(board->getLCD(), board->getTouch());

    Serial.println("UI olusturuluyor...");
    lvgl_port_lock(-1);

    ui_init();

    lvgl_port_unlock();
    Serial.println("UI tamamlandi! Ekran hazir.");
}

void loop()
{
    delay(5);
}