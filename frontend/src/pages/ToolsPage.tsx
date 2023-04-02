import "./ToolsPage.scss"

export default function ToolsPage() {
  return (
    <div class="toolsPage">
      <div class="contentHeader">
        Tools
        <div class="contentHeaderDescription">Useful for troubleshooting and managing your install</div>
      </div>
      <div class="buttonContainer">
        <div class="button" id="uninstall">Uninstall app</div>
        <div class="buttonLabel">Uninstalls the app you selected (needs to be confirmed on android device)</div>
      </div>
      <div class="buttonContainer">
        <div class="button" id="changeApp">Change app</div>
        <div class="buttonLabel">Change the app you want to manage</div>
      </div>
      <div class="buttonContainer">
        <div class="button" id="requestAppPermission">Request app directory permission</div>
        <div class="buttonLabel">Requests permissions to access the currently selected apps data directory</div>
      </div>
      <div class="buttonContainer">
        <div class="button" id="requestAppObbPermission">Request app obb permission</div>
        <div class="buttonLabel">Requests permissions to access the currently selected apps obb directory</div>
      </div>
      <div class="buttonContainer">
        <div class="button" id="requestManageStorageAppPermission">Allow manage storage permission for selected app</div>
        <div class="buttonLabel">Opens settings to allow manage storage for the selected app</div>
      </div>
      <div class="buttonContainer">
        <div class="button" id="checkUpdate">Check for QuestAppVersionSwitcher updates</div>
        <div class="buttonLabel">Checks if updates for QuestAppVersionSwitcher exist</div>
      </div>
      <div id="updateTextBox" class="textBox"></div>

      <div class="space">
        <div class="contentHeader">
          Login Section
          <div class="contentHeaderDescription">Login for downgrading games</div>
        </div>
        <b id="loggedInMsg" >You are logged in. You can login again if you want to reset your password.</b>
        <div class="buttonContainer">
          <div class="button" id="login">Login</div>
          <div class="buttonLabel">Log in with your Oculus/Facebook account to downgrade games (<b>ONLY WORKS ON QUEST, NOT ON PC</b>)</div>
        </div>
        <div class="headerMargin">
          {/* <a onclick="document.getElementById('tokenLoginContainer').style.display = 'block'" class="underline">Other login methods</a></div> */}
          <div id="tokenLoginContainer" style="display: none;">
            <div class="buttonContainer">
              <input type="password" id="tokeninput" placeholder="Token" />
              <div class="button" id="logintoken">Login with token</div>
              <div class="buttonLabel">Log in with your Oculus token to downgrade games (Open this page on PC or phone to paste your token; Type the link shown at the bottom of this page in your browser)</div>
            </div>
            <a href="https://computerelite.github.io/tools/Oculus/ObtainToken.html">Guide to get your token</a>
            <div id="tokenTextBox" class="textBox"></div>
          </div>
        </div>

        <div class="space">
          <div class="contentHeader">
            Server control
            <div class="contentHeaderDescription">Configure the QuestAppVersionSwitcher WebServer</div>
          </div>
          <div class="buttonContainer">
            <div class="button" id="exit">Exit</div>
            <div class="buttonLabel">Exits QuestAppVersionSwitcher</div>
          </div>
          <div class="buttonContainer">
            <input type="number" placeholder="50002" id="port" class="buttonLabel" value="50002" style="width: 100px;" />
            <div class="button" id="confirmPort">Change port</div>
            <div class="buttonLabel">Changes the WebServer port</div>
          </div>
          <div id="serverTextBox" class="textBox"></div>
        </div>

        <div class="space">
          <div class="contentHeader">
            Help
            <div class="contentHeaderDescription">Utilities to help debugging QuestAppVersionSwitcher</div>
          </div>
          <div class="buttonContainer">
            <input type="password" placeholder="Enter QuestAppVersionSwitcher password" id="logspwd" class="buttonLabel" value="" style="width: 300px;" />
            <div class="button" id="logs">Upload logs</div>
            <div class="buttonLabel">ONLY do when instructed to do so. Uploads which apps you own to OculusDB for viewing by support members</div>
          </div>
          <div id="logsText" class="textBox"></div>
        </div>

        <div class="space">
          All backups take up <b class="inline totalSize"></b> of space on your device in total.
          <br />
          You can delete backups in the <code>Backup</code> section
        </div>
        <div class="about">
          Quest App Version Switcher version <div id="version" class="inline"></div>
          Accessible via browser at:
          <div id="ips">

          </div>
        </div>
      </div>
    </div>
  )
}
