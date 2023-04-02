import { MetaProvider, Title, Link, Meta } from '@solidjs/meta';

export default function BackupPage() {
    return (
        <div>
            <Title>sdfsafsda</Title>
            <div class="contentHeader">
                Backup
                <div class="contentHeaderDescription">Backup your game and restore backups</div>
            </div>
            <div class="buttonContainer">
                <div class="button" id="changeApp2">Change app</div>
                <div class="buttonLabel">Change the app you want to manage</div>
            </div>
            <div class="topMargin">Backups of <div class="inline packageName">some game</div> (<div id="size" class="inline"></div>)</div>
            <div id="backupList" class="list">
                
            </div>


            <div class="contentHeader headerMargin">
                Restore backups
                <div class="contentHeaderDescription">Restores the backup selected above</div>
                <br />
                <b class="hidden" id="onPcInfo">You can only restore backups in headset</b>
                <label><input type="checkbox" id="restoreAppData" value="only restore app data" style="width: auto;" />Only restore app data</label>
                <div class="buttonContainer">
                    <div class="button" id="restoreBackup">Restore backup</div>
                    <div class="buttonLabel">Restores the selected backup</div>
                </div>
                <div class="buttonContainer">
                    <div class="button" id="deleteBackup">Delete backup</div>
                    <div class="buttonLabel">Deletes the selected backup</div>
                </div>
                <div id="restoreTextBox" class="textBox"></div>
            </div>

            <div class="contentHeader headerMargin">
                Create backups
                <div class="contentHeaderDescription">Change the app at the top</div>
            </div>
            <div>
                <input type="text" placeholder="Backup Name" id="backupname" />
                <div class="buttonContainer">
                    <div class="button" id="createBackup">Create backup</div>
                    <div class="buttonLabel">Creates a backup of <div class="inline packageName">some game</div></div>
                </div>
                <label><input type="checkbox" id="appdata" value="only backup app data" style="width: auto;" />Only backup app data</label>
                <div id="backupTextBox" class="textBox"></div>
            </div>
        </div>
    )
}
