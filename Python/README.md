# Overview
This code runs using some forked and/or pinned versions of the crazyflie libraries, some
with modifications that allow features such as compressed trajectories and broadcasting
via the C++ link.

These libraries are included in the repository as submodules, and must be cloned, built/compiled
and installed for

# 1. Prerequisites
- Windows 10
- Python 3.9
- `pip` & `virtualenv`
- Visual Studio 2019 (Community or Pro)

## Submodules
Make sure you've run the following to clone & setup the submodules:
```bash
git submodule init
git submodule update
```
- `libusb`
- `crazyflie-lib-python`
- `crazyflie-link-cpp`
- `crazyflie-firmware`
- `crazyradio-firmware`

# 2. Build binary artifacts 

## Building libusb
For some reason, the prebuilt version of `libusb` crashes with the C++ link code, so you have to build it from the source.
Fortunately, this is very straightforward to do. Once the `libusb` submodule is initialized and updated, navigate to 
`Python/libusb/msvc` in a Windows Explorer and open up the solution file `libusb_2019.sln` in Visual Studio.

Make sure to change the current configuration to `Release` & `x64`, and then build the solution. Assuming things work,
you should now find a `libusb-1.0.lib` file at `Python/libusb/x64/Release/lib`. Keep note of this file, it will be used
in the next step.

## Building crazyflie-link-cpp
Navigate to `Python/crazyflie-link-cpp/` from a GitBash terminal. First make sure you've set up the `pybind11` submodule
for the C++ python bindings:
```shell
git submodule init
git submodule update
```

After than, run the following
```shell
`mkdir build`
cd build
cmake ..
```
This should result in a `/build/` folder containing Visual C++ project and solution files, and you should be able to open 
up `crazyflie-link-cpp.sln` in Microsoft Visual Studio. 

Before building the library, you should replace the `libusb-1.0.lib` file at `Python/crazyflie-link-cpp/build/3rdparty/libusb-1.0.24/VS2019/MS64/static` 
with the one from the previous step, (at `Python/libusb/x64/Release/lib`). Make sure this solution is also set to 
`Release` and `x64`, and then you can build the library using Build > Build Solution. You should see build artifacts
in `Python/crazyflie-link-cpp/build/Release`, with the important one being the file **`cflinkcpp.cp39-win_amd64.pyd`**

## Building  crazyradio-firmware
Unfortunately there's no up to date release of the Crazyradio firmware that has the broadcast functionality,
so this firmware must be compiled from the source code. To build this, first install the SDCC toolkit
(Small Device C Compiler) for Windows, which has an 64-bit installer available [here](https://sourceforge.net/projects/sdcc/files/sdcc-win64/).

Then, from a Powershell terminal, navigate to `Python/crazyradio-firmware/firmware` and run
```bash
make CRPA=1
```

This should output a `crazyradio.bin` file  in the `Python/crazyradio-firmware/firmware/bin` directory, which is what will
be needed to flash the radio firmware.

## Building crazyflie-firmware
A pre-built binary exists at `Python/firmware-tools/crazyflie-firmware/cf2.bin`, but if for whatever reason a new version needs
to be built, see the [README instructions](./crazyradio-firmware/README.md) in the module.

# 3. Setup Python environment

## Create & activate a virtualenv
From a GitBash Shell (not Windows Powershell) Navigate to `Python/`
```bash
py -m venv venv
source ./venv/Scripts/activate
```
If using PyCharm, make sure to set use the Python interpreter from the virtualenv.

## Install dependencies
From the `Python/` directory, while the virtualenv is active, run:
```bash
pip install -r requirements.txt
```
and then run the following to copy the custom built version of `cflinkcpp` to the `site-packages-directory` 
(assuming you've built `cflinkcpp` as described above):
```shell
cp ./crazyflie-link-cpp/build/Release/cflinkcpp.cp39-win_amd64.pyd ./venv/Lib/site-packages/cflinkcpp.cp39-win_amd64.pyd
```

# 4. Radio Driver & libusb
To run on Windows using the C++ radio library (`crazyflie-link-cpp`), the `libusbK` radio driver **must**
be installed (not the `libusb-win32` driver that is recommended by default). The `WinUSB` driver will
work as well, but the `libusbK` works with the default Python link, where as `WinUSB does not:
- `libusb-win32`: supports Python link, does **not** support C++ link or radio bootloader scripts
- `WinUSB`: supports C++ link and radio bootloader, does **not** support Python link
- `libusbK`: supports all 3

The C++ link will seg-fault if the `WinUSB` or `libusbK` driver is not used, and updating the driver can be done using 
Zadig (see instructions on crazyflie website).

Additionally, the `libusb-1.0.dll` file in `C:\Windows\System32` must be replaced with the one from
`firmware-tools/` in the repository, and this must be done before attempting to flash or connect to the radio.

# 5. Flashing Firmware
After setting up the Python environment & building the libraries for the software,
the firmware on the individual drones needs to be flashed, as well as the firmware
for the radio dongle, in order to support broadcasting.

## Crazyradio Firmware
To flash the radio firmware, make sure the `libusbK` driver for the radio dongle has been installed
via Zadig. Navigate to `crazyradio-firmware/firmware` from a Git Bash shell and run
```bash
python ../usbtools/launchBootloader.py
```
This will launch the radio into firmware mode (provided the `libusb-1.0.dll` file & `libusbK` driver have been installed).

**NOTE:** At this point, the radio will no longer appear as a `Crazyradio PA dongle`, and will instead start appearing
as an `nRF24... BOOT LDR` device, and so you if you have not already done so, 
you **must** install a driver for this device using Zadig (should install the same `libusbK` driver).
No driver will be installed by default and `libusb` will not know how to open and communicate with the device by default,
so attempting to flash before installing a driver will result in the script throwing a `NotImplementedError`.

Once the driver is installed, from the same directory, you can then run:
```bash
python ../usbtools/nrfbootload.py flash bin/cradio.bin
```
Make sure to then unlpug and plug the radio back in, to launch it back in firmware (normal operating) mode.

## Crazyflie Firmware & NRF24 Firmware
To flash the main crazyflie firmware, a Git Bash shell, navigate to `Python/firmware-tools/` in the repository and run
```bash
python -m cfloader -w radio://0/55/2M/E7E7E7E7E7 flash crazyflie-firmware/cf2.bin stm32-fw
```

Flashing the NRF 24 firmware follows a very similar process. From the same directory, run:
```bash
python -m cfloader -w radio://0/55/2M/E7E7E7E7E7 flash crazyflie2-nrf-firmware/cf2_nrf-2021.03.bin nrf51-fw
```
***NOTE:** For both of these steps, make sure to update the channel/address to match the crazyflie that is currently being flashed.*


# 6. Crazyflie Addresses
The crazyfiles should configured each to have the same channel (ex `55`), and unique addresses. 
The convention is to use addresses in the form `E7E7E7E7XX` (ex `E7E7E7E701`). 
The easiest way to do this is to use the Crazyflie PC Client—after connecting to a crazyflie, go to
`Connect > Configure 2.X`, which should open a dialog box that allows changing both the channel and 
address for the crazyflie.

## Broadcasting
To broadcast to all crazyflies on the channel, send a broadcast message with address `FFE7E7E7E7`.
This will only work if all 3 firmwares have been upgraded to the right version (crazyflie main firmware, crazyflie nrf24 firmware,
 and the crazyradio firmware). 

# TODO - DESCRIBE PROCESS OF OPENING A BROADCAST LINK IN PYTHON