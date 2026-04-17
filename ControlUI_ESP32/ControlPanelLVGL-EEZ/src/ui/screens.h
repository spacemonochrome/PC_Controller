#ifndef EEZ_LVGL_UI_SCREENS_H
#define EEZ_LVGL_UI_SCREENS_H

#include <lvgl.h>

#ifdef __cplusplus
extern "C" {
#endif

// Screens

enum ScreensEnum {
    _SCREEN_ID_FIRST = 1,
    SCREEN_ID_MAIN = 1,
    _SCREEN_ID_LAST = 1
};

typedef struct _objects_t {
    lv_obj_t *main;
    lv_obj_t *ses_slider;
    lv_obj_t *ekran_slider;
    lv_obj_t *obj0;
    lv_obj_t *obj1;
    lv_obj_t *cpu_usage_value;
    lv_obj_t *gpu_usage_value;
    lv_obj_t *obj2;
    lv_obj_t *pil_progressbar;
    lv_obj_t *obj3;
    lv_obj_t *obj4;
    lv_obj_t *obj5;
    lv_obj_t *obj6;
    lv_obj_t *temp_chart;
    lv_obj_t *pil_value;
    lv_obj_t *ses_value;
    lv_obj_t *ekran_value;
    lv_obj_t *stop_button;
    lv_obj_t *durate_button;
    lv_obj_t *start_button;
    lv_obj_t *obj7;
    lv_obj_t *led;
} objects_t;

extern objects_t objects;

typedef struct {
    lv_meter_scale_t *scale;
    lv_meter_indicator_t *indicator;
    lv_meter_scale_t *scale1;
    lv_meter_indicator_t *indicator1;
} screen_main_state_t;

extern screen_main_state_t screen_main_state;

void create_screen_main();
void tick_screen_main();

void tick_screen_by_id(enum ScreensEnum screenId);
void tick_screen(int screen_index);

void create_screens();

#ifdef __cplusplus
}
#endif

#endif /*EEZ_LVGL_UI_SCREENS_H*/