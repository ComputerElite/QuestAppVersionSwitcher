# QuestAppVersionSwitcher
Allows you to backup and restore apps directly from withing your quest

## Building this project

To start, you'll need to install [Visual Studio 2019 with xamarin](https://docs.microsoft.com/en-us/xamarin/get-started/installation/?pivots=windows).

Then clone this project and it's supporting library:

```bash
$ mkdir -p projects/quest
$ git clone https://github.com/ComputerElite/QuestAppVersionSwitcher.git projects/quest/QuestAppVersionSwitcher
$ git clone https://github.com/ComputerElite/ComputerUtils.git projects/ComputerUtils
```

- Open QuestAppVersionSwitcher in Visual Studio, then go to `Tools -> Android -> Android SDK Manager`
- Expand `Android 10.0 - Q` and select `Android SDK Platform 29`, then hit `Apply Changes`
- Once it's installed, a license windows will appear. Hit `accept`
- In the Solution Explorer, right click `Solution 'QuestAppVersionSwitcher'` and select `Add -> Existing Project`
- Navigate to `ComputerUtils\ComputerUtils.Android\ComputerUtils.Android.csproj` and select it
- Build the project with `Build -> Build Solution`
- In the Solution Explorer, right click `QuestAppVersionSwitcher` and select `Archive...`
- Click `Distribute`, `Adhoc`, then create a Signing Identity if you don't already have one ((more info[https://docs.microsoft.com/en-us/xamarin/android/deploy-test/signing/?tabs=windows]))
- Select your Signing Indentity, then click `Save As` and pick where you'd like your signed apk to be saved
- Enter your Signing Identity password
- Select `Open Folder`, then your signed apk is in the `signed-apks` folder. You can now install it with SideQuest!
