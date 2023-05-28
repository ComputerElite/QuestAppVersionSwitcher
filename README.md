# Quest App Version Switcher

## Building

### Requirements

- [Node.js](https://nodejs.org/en/) (v18.0.0 or higher)
- [Yarn](https://yarnpkg.com/) (v1.22.10 or higher)
- [Visual Studio](https://visualstudio.microsoft.com/)
- [Xamarin](https://visualstudio.microsoft.com/xamarin/)
- [Android SDK](https://developer.android.com/studio) (API Level 32 - Android 12L)

*Add MSBuild to your PATH environment variable. You can find the path to MSBuild in Visual Studio Installer.*

### Prerequisites

This project uses computerelite utils, you need to clone them next to this project.

```bash
git clone https://github.com/ComputerElite/ComputerUtils.git
```

It will create a folder next to this project called `ComputerUtils`.

### Build the web app

```bash
cd frontend/
yarn install
yarn build
```

### Build the debug apk from command line

```bash
msbuild /t:PackageForAndroid /t:SignAndroidPackage /p:Configuration=Debug
```
*The apk will be located at `QuestAppVersionSwitcher\bin\Debug\com.ComputerElite.questappversionswitcher-Signed.apk`*

### Build the release apk from command line

```bash
msbuild /t:PackageForAndroid /t:SignAndroidPackage /p:Configuration=Release
```

*The apk will be located at `QuestAppVersionSwitcher\bin\Debug\com.ComputerElite.questappversionswitcher-Signed.apk`*

### Forward ports from android emulator to host machine

Frontend development is done on the port 3000, for it we need to forward the port from the android emulator to the host machine.
Run following commands in the terminal to forward needed ports:
We are not using the ports 50002 and 50003 on the host machine because windows uses it sometimes and it fails to forward it.

```bash
# Forward web app port
adb forward tcp:5002 tcp:50002

# Forward websocket port (for the development version) 
adb forward tcp:3001 tcp:50003
```