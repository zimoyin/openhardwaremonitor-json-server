# OpenHardwareMonitorJsonServer
This project is a spike for a simple JSON server that serves up data about your computer's hardware, using [OpenHardwareMonitor](https://github.com/openhardwaremonitor/openhardwaremonitor).

![](https://i.imgur.com/UWfV3o7.jpg)

## Example JSON

https://gist.github.com/danieltian/8a63669e589dcc441d26

## Prerequisites
- .NET Framework 4.5.2

## Install and Run
You must run Visual Studio (or the exe in the `Bin/Release` folder) as an administrator; otherwise, the HTTP server won't run properly and you will get an `Access denied` exception. This repo comes with all the required libraries, and you should be able to compile and run it with no additional setup.

## Why?
Although OpenHardwareMonitor's GUI has a JSON server feature (under **Options -> Remote Web Server**, then accessible through `http://localhost:<port>/data.json`), it's pretty heavyweight if all you want is the JSON data for the sensors.

Also, OpenHardwareMonitor hasn't seen a release on their [official site](http://openhardwaremonitor.org/) in over a year and lacks support for the latest hardware. The [Github repo](https://github.com/openhardwaremonitor/openhardwaremonitor) is updated regularly though, and does have support for the latest and greatest. The copy of `OpenHardwareMonitor.dll` in this project was built off master from the Github repo as of 2016/06/14.

----
下面提供一份详细的文档说明，通过表格的方式描述如何对接此 JSON 数据以及如何解析各个字段，帮助开发者将硬件监控数据集成到自己的应用中。

---

## 1. 接口信息

| 项目         | 说明                                                  |
| ------------ | ----------------------------------------------------- |
| 请求 URL     | http://127.0.0.1:8080/                                |
| 请求方式     | HTTP GET                                              |
| 返回格式     | JSON 数据                                             |

---

## 2. JSON 数据整体结构

| 顶层键       | 描述                           |
| ------------ | ------------------------------ |
| Mainboard    | 主板信息                       |
| CPU          | CPU（中央处理器）信息          |
| RAM          | 内存（RAM）数据                |
| GpuNvidia    | NVIDIA 显卡信息                |
| HDD          | 硬盘（磁盘）信息               |

---

## 3. 主板（Mainboard）数据结构

| 字段           | 类型     | 说明                                 |
| -------------- | -------- | ------------------------------------ |
| Name           | 字符串   | 主板名称（例如：Lenovo LNVNB161216） |
| Identifier     | 字符串   | 设备唯一标识符（例如：/mainboard）   |
| HardwareType   | 数字     | 硬件类型编号（0 表示主板）           |
| Parent         | null     | 父设备信息（主板一般无父设备）       |
| SubHardware    | 数组     | 子硬件列表（目前为空数组）           |
| Sensors        | 数组     | 传感器数据列表（目前为空数组）       |

---

## 4. CPU 数据结构

### 4.1 CPU 基本信息

| 字段                        | 类型     | 说明                                                     |
| --------------------------- | -------- | -------------------------------------------------------- |
| Name                        | 字符串   | CPU 名称（例如：AMD Ryzen 5 5600H）                      |
| Identifier                  | 字符串   | 设备标识符（例如：/amdcpu/0）                             |
| HardwareType                | 数字     | 硬件类型编号（2 表示 CPU）                               |
| HasModelSpecificRegisters   | 布尔     | 是否支持特定寄存器                                       |
| HasTimeStampCounter         | 布尔     | 是否支持时间戳计数器                                     |
| TimeStampCounterFrequency   | 数值     | 时间戳计数器频率（例如：3293.7214576025804）              |
| SubHardware                 | 数组     | 子硬件列表（目前为空数组）                               |
| Sensors                     | 数组     | CPU 相关的传感器数据（包括负载、功率、温度、时钟等）       |

### 4.2 CPU 传感器字段说明

| 字段           | 类型   | 说明                                                          |
| -------------- | ------ | ------------------------------------------------------------- |
| SensorType     | 数字   | 传感器类型编号（如 1：时钟、2：温度、3：负载、9：功率）        |
| Identifier     | 字符串 | 传感器唯一标识符（例如：/amdcpu/0/load/1 表示第 1 核负载）       |
| Name           | 字符串 | 传感器名称（例如：CPU Core #1）                                |
| Index          | 数值   | 传感器索引，用于区分同类传感器                                |
| IsDefaultHidden| 布尔   | 是否默认隐藏（前端显示时可依据此字段过滤）                     |
| Value          | 数值   | 当前传感器读数（例如：负载百分比、功率数值、温度等）              |
| Min            | 数值   | 传感器最小可能值                                              |
| Max            | 数值   | 传感器最大可能值                                              |
| Control        | 任意   | 控制字段，目前一般为 null，可用于未来扩展（如风扇控制等）         |

---

## 5. 内存（RAM）数据结构

| 字段           | 类型   | 说明                                             |
| -------------- | ------ | ------------------------------------------------ |
| Name           | 字符串 | 内存名称（例如：Generic Memory）                 |
| Identifier     | 字符串 | 设备标识符（例如：/ram）                          |
| HardwareType   | 数字   | 硬件类型编号（3 表示内存）                        |
| SubHardware    | 数组   | 子硬件列表（目前为空数组）                        |
| Sensors        | 数组   | 内存传感器数据，包含负载、已使用内存和可用内存信息  |

### 内存传感器说明

| 传感器字段    | SensorType 数值 | 说明                                 |
| ------------- | --------------- | ------------------------------------ |
| 内存负载      | 3               | 内存整体使用负载（百分比）           |
| Used Memory   | 10              | 已使用内存（数值，单位视情况而定）    |
| Available Memory | 10           | 可用内存（数值，单位视情况而定）       |

---

## 6. NVIDIA 显卡（GpuNvidia）数据结构

| 字段           | 类型   | 说明                                                                |
| -------------- | ------ | ------------------------------------------------------------------- |
| Name           | 字符串 | 显卡名称（例如：NVIDIA NVIDIA GeForce RTX 3050 Ti Laptop GPU）       |
| Identifier     | 字符串 | 设备标识符（例如：/nvidiagpu/0）                                     |
| HardwareType   | 数字   | 硬件类型编号（4 表示显卡）                                           |
| SubHardware    | 数组   | 子硬件列表（目前为空数组）                                           |
| Sensors        | 数组   | 显卡传感器数据，包含温度、核心时钟、显存频率、负载和显存使用信息       |

### 显卡传感器说明（部分）

| 传感器字段  | SensorType 数值 | 说明                                 |
| ----------- | --------------- | ------------------------------------ |
| GPU Core    | 2               | GPU 核心温度（摄氏度）               |
| GPU Core    | 1               | GPU 核心时钟频率（MHz）              |
| GPU Memory  | 1               | GPU 显存时钟频率（MHz）              |
| GPU Load    | 3               | GPU 负载（百分比）                   |
| GPU Memory  | 11              | 显存使用数据（总、已用、剩余）         |

---

## 7. 硬盘（HDD）数据结构

| 字段           | 类型   | 说明                                              |
| -------------- | ------ | ------------------------------------------------- |
| Name           | 字符串 | 硬盘名称（例如：Generic Hard Disk）               |
| Identifier     | 字符串 | 设备标识符（例如：/hdd/0、/hdd/1 等）             |
| HardwareType   | 数字   | 硬件类型编号（8 表示硬盘）                         |
| SubHardware    | 数组   | 子硬件列表（目前为空数组）                         |
| Sensors        | 数组   | 硬盘传感器数据，主要关注磁盘已使用空间百分比信息      |

### 硬盘传感器说明

| 传感器字段  | SensorType 数值 | 说明                 |
| ----------- | --------------- | -------------------- |
| Used Space  | 3               | 硬盘已使用空间百分比  |

---

## 8. 传感器类型对应说明

| SensorType 数值 | 描述说明                     |
| --------------- | ---------------------------- |
| 1               | 时钟频率（单位：MHz）         |
| 2               | 温度（单位：摄氏度）          |
| 3               | 负载或使用率（百分比）        |
| 9               | 功率（单位：瓦特）            |
| 10              | 内存相关数据（内存使用情况）  |
| 11              | 显存相关数据（总、已用、剩余）  |

---

## 9. 数据解析与对接流程概述

| 步骤         | 说明                                                                               |
| ------------ | ---------------------------------------------------------------------------------- |
| 数据请求     | 使用 HTTP GET 请求至指定 URL（http://127.0.0.1:8080/）获取监控数据                  |
| 状态检查     | 检查返回状态码是否为 200，确保数据正常返回                                           |
| JSON 解析    | 将返回的 JSON 数据解析为对象，并识别顶层键（Mainboard、CPU、RAM、GpuNvidia、HDD）       |
| 硬件遍历     | 分别遍历每个硬件设备数组，读取设备基本信息（Name、Identifier、HardwareType 等）        |
| 传感器处理   | 针对每个硬件的 Sensors 数组，根据 SensorType 对数据进行分类处理（如负载、温度、时钟、功率）|
| 异常处理     | 针对网络请求失败或数据解析异常情况，增加错误处理逻辑，保证系统健壮性                     |

---

## 10. 扩展说明

| 项目           | 说明                                                         |
| -------------- | ------------------------------------------------------------ |
| 子硬件支持     | 若设备包含子硬件（SubHardware 数组不为空），需递归遍历解析每个子设备。  |
| 控制字段       | Sensors 中的 Control 字段目前通常为 null，可预留扩展用于控制操作（如风扇调速）。 |
| 数据实时性     | 监控数据会实时更新，应用中可根据需要设置定时刷新机制。           |
| 前端显示       | 根据 IsDefaultHidden 标识，决定是否在前端展示某些传感器数据。      |

---

通过以上各表格说明，开发者可以全面了解 JSON 数据的各个部分、字段含义及对接流程，从而便于在系统中进行数据解析、展示及扩展。

