# ESP32-S3 OBS Studio Touch Controller

**A complete real-time OBS Studio control and PC hardware monitoring system using a 5-inch touchscreen LCD on ESP32-S3.**

This project combines a modern LVGL-based touchscreen interface on the **ESP32-S3 + Waveshare 5B LCD** with a powerful Windows application to give you full control of OBS Studio and live monitoring of your PC's hardware (CPU, GPU, temperatures, battery, audio, etc.).

---

## ✨ Features

### ESP32 Touchscreen Interface
- Full OBS Studio control: **Start Recording**, **Pause/Resume**, **Stop**
- Real-time **Volume Slider** with live feedback
- Real-time **Screen Brightness Slider**
- CPU & GPU usage meters with animated needle gauges
- Live **CPU & GPU Temperature Chart**
- Battery level indicator
- Status LED indicator (Recording = Red, Paused = Orange, Idle = Off)
- Beautiful, smooth LVGL v8 user interface with custom font and background image

### Windows PC Application
- Full **OBS WebSocket** integration
- Real-time hardware monitoring via **LibreHardwareMonitor**
- System audio control via **NAudio**
- Stable high-speed UART communication with ESP32
- Automatic synchronization of OBS state between PC and ESP32

---

## 🛠️ Hardware Requirements

- **ESP32-S3 Development Board** (recommended: Waveshare ESP32-S3-Touch-LCD-5B)
- **5" RGB LCD Panel** (1024×600 resolution)
- **GT911 Touch Controller**
- **CH422G IO Expander** (used for backlight control and LCD reset)
- USB-to-UART connection between PC and ESP32

---

## 📦 Software Requirements

### ESP32 Side
- Arduino IDE or PlatformIO
- ESP32 Arduino Core
- Required Libraries:
  - `ESP32_Display_Panel`
  - `lvgl` (Version 8)
  - Custom `lvgl_v8_port`

### Windows PC Side
- **.NET 8.0 or .NET 10.0 Windows**
- Visual Studio 2022 (recommended)
- NuGet Packages:
  - `LibreHardwareMonitorLib`
  - `NAudio`
  - `obs-websocket-dotnet`

---

## 📡 Communication Protocol (UART)

The ESP32 and PC communicate over **115200 baud UART** using a custom 29-byte packet protocol.

### PC → ESP32 (Main Status Packet)
```text
SES:PAR:CPU:GPU:CTP:GTP:PIL:K\n
