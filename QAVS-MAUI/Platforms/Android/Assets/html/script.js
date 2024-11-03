﻿const patchStatus = document.getElementById("patchStatus");
const squareLoader = `
<div class="loaderContainer">
    <div class="loaderBarRight"></div>
    <div class="loaderBarLeft"></div>
    <div class="loaderBarTop"></div>
    <div class="loaderBarBottom"></div>
    <div class="loaderSpinningCircle"></div>
    <div class="loaderMiddleCircle"></div>
    <div class="loaderCircleHole"></div>
    <div class="loaderSquare"></div>
</div>`
var diffDowngradeEnabled = true

fetch("/api/downgrade/usediff").then(res => res.json().then(res => {
    diffDowngradeEnabled = res.useDiff
    document.getElementById("downgradeMenuButton").setAttribute("section", diffDowngradeEnabled ? "downgradeDiff" : "downgrade")

    if(diffDowngradeEnabled) {
        setTimeout(() => {
            UpdateDowngrades()
        }, 2500)
    }
    ReloadDowngradeIFrame();
}))

document.getElementById("useDiffDowngrade").onclick = () => {
    var checked = document.getElementById("useDiffDowngrade").checked
    fetch("/api/downgrade/usediff", {method: "POST", body: checked})
}

var currentGameVersion = ""

const browser = document.getElementById("browser")
const toastsE = document.getElementById("toasts")
ReloadDowngradeIFrame();
function ReloadDowngradeIFrame() {
    if(diffDowngradeEnabled) return
    document.getElementById("downgradeframe").src = `https://oculusdb.rui2015.me/search?query=Beat+Saber&headsets=MONTEREY%2CHOLLYWOOD${IsOnQuest() ? `&isqavs=true` : ``}`
}
// connect to websocket one port higher than the server
var socket = new WebSocket("ws://" + window.location.hostname + ":" + (parseInt(window.location.port) + 1) + "/");
socket.onerror = function (error) {
    console.log("WebSocket Error: " + error + ". Reconnecting...");
    // reconnect
    socket = new WebSocket("ws://" + window.location.hostname + ":" + (parseInt(window.location.port) + 1) + "/");
}

socket.onclose = function (e) {
    console.log("WebSocket closed. Reconnecting...");
    // reconnect
    socket = new WebSocket("ws://" + window.location.hostname + ":" + (parseInt(window.location.port) + 1) + "/");
}

function DownloadDowngrade(package, sourceSha, targetSha, targetVersion) {
    fetch("/api/downloaddiff", {
        method: "POST", body: JSON.stringify({
            packageName: package,
            sourceSha: sourceSha,
            targetSha: targetSha,
            targetVersion: targetVersion
        })
    })
    OpenTab("download")
}
function UpdateDowngrades() {
    document.getElementById("compatibleDowngrades").innerHTML = `Loading downgrades${squareLoader}`
    fetch(`https://raw.githubusercontent.com/ComputerElite/APKDowngrader/main/versions.json`).then(res => res.json().then(res => {
        // Check if current app even has any downgrades
        if(!res.versions.find(x => x.appid == config.currentApp)) {
            document.getElementById("allDowngradeShit").style.display = "none"
            document.getElementById("noDowngradeShit").style.display = "block"
            document.getElementById("noDowngradeShit").innerHTML = `There are no patches available for <code>${config.currentApp}</code>. This means you can sadly not downgrade currently.
<br>
<br>If you ask yourself why it worked before or works in a video you're watching, here's the reason:4
<br>Meta changed how downloads work and I haven't figured out a way how to download any version of any app yet. If we find a way to do that, I'll add it back. But for now you can only downgrade specific games to specific versions which is all manual work on our side to get working.
<br>I'm sorry for the inconvenience.`
            return
        }
        document.getElementById("allDowngradeShit").style.display = "block"
        document.getElementById("noDowngradeShit").style.display = "none"
        config.currentApp
        document.getElementById("compatibleDowngrades").innerHTML = `Checking compatibility. This may take 2 minutes${squareLoader}`
        fetch(`/api/currentsha256`).then(s => s.json().then(s => {
            var sha = s.msg
            var compatible = []
            var available = []
            for(const version of res.versions) {
                if(version.appid == config.currentApp) {
                    if(version.SSHA256 == sha) {
                        compatible.push(version)
                    } else {
                        available.push(version)
                    }
                }
            }
            var html = ""
            if(compatible.length > 0) {
                html = `<tr>
                            <th>Version</th>
                            <th></th>
                            <th>Info</th>
                        </tr>`
                for(const version of compatible) {
                    html += `
                    <tr>
                        <td>${version.TV}</td>
                        <td><div onclick="DownloadDowngrade('${config.currentApp}', '${version.SSHA256}', '${version.TSHA256}', '${version.TV}')" class="button">Download</div></td>
                        <td>${version.Annotation ? version.Annotation : "None"}</td>
                    </tr>`
                }
            } else {
                html = "No compatible versions found for currently installed version. Please refer to below"
            }
            document.getElementById("compatibleDowngrades").innerHTML = html

            if(available.length > 0) {
                html = `<tr>
                            <th>Needed Version</th>
                            <th>Target Version</th>
                            <th>Info</th>
                        </tr>`
                for(const version of available) {
                    html += `
                    <tr>
                        <td>${version.SV}</td>
                        <td>${version.TV}</td>
                        <td>${version.Annotation ? version.Annotation : "None"}</td>
                    </tr>`
                }
            } else {
                html = "No other downgrades available for this game."
            }
            document.getElementById("availableDowngrades").innerHTML = html
            
        }))
    })).catch(e => {
        document.getElementById("compatibleDowngrades").innerHTML = `<span style="color: #EE0000;">Error loading downgrades</span>`
    })
    fetch(`/api/patching/getmodstatus`).then(res => res.json().then(res => {
        document.getElementById("currentVersionDowngrade").innerHTML = `Current installed version: ${res.version} ${res.isPatched ? "(Patched)" : ""}`
    }))
}

function FormatBytes(bytes, decimals = 2) {
    if (bytes > 1099511627776) return (input / 1099511627776.0).toFixed(decimals) + " TB";
    // GB
    else if (bytes > 1073741824) return (input / 1073741824.0).toFixed(decimals) + " GB";
    // MB
    else if (bytes > 1048576) return (input / 1048576.0).toFixed(decimals) + " MB";
    // KB
    else if (bytes > 1024) return (input / 1024.0).toFixed(decimals) + " KB";
    // Bytes
    else return bytes + " Bytes";
}
    

socket.onmessage = function (e) {
    var data = JSON.parse(e.data);
    if(data.route == "/api/downloads") {
        UpdateDownloads(data.data)
    } else if(data.route == "/api/mods/mods") {
        modStatus = data.data
        UpdateMods()
    } else if(data.route == "/api/patching/patchstatus") {
        UpdatePatchStatus(data.data)
    } else if(data.route.startsWith("/api/mods/operation/")) {
        var i = modStatus.operations.indexOf(modStatus.operations.find(x => x.operationId == data.data.operationId))
        if(i == -1) {
            modStatus.operations.push(data.data)
        } else {
            modStatus.operations[i] = data.data
        }
        UpdateMods()
    } else if(data.route == "/api/mods/modloader") {
        UpdateModLoader(data.data)
    }
}

function FetchModLoader() {
    fetch("/api/mods/modloader").then(res => res.json().then(res => {
        UpdateModLoader(res)
    }))
}

function UpdateModLoader(data) {
    modloader = data.modloader
    UpdateInstalledModsQuest3Hint()
    if(lastCheckedAccessForGame != config.currentApp) {
        lastCheckedAccessForGame = config.currentApp
        if(modloader == 0) { // we only need access to android dir for QuestLoader
            fetch("/api/gotaccess?package=" + config.currentApp).then(res => {
                res.json().then(j => {
                    if (!j.gotAccess && !params.get("noaccesscheck")) {
                        // Open need access prompt
                        CloseGetPasswordPopup();
                        OpenRestorePopup();
                        GotoStep("12")
                    }
                })
            })
        }
    }
}

function UpdatePatchStatus(j) {
    if (j.done) {
        if(scotlandForever) scotlandForever.pause()
        TextBoxGood("patchingTextBox", j.currentOperation)
        patchInProgress = false
        if(j.backupName) {
            if(!IsOnQuest()) {
                alert("Restore the backup " + j.backupName + " from within your Quest to finalize the patching process")
                return
            }
            // patching returned a backup name to restore so restore the backup
            selectedBackup = j.backupName
            ChangeApp(j.package).then(res => {
                OpenRestorePopup();
            })
        }
        UpdateUI()

    } else if (j.error) {
        if(scotlandForever) scotlandForever.pause()
        TextBoxError("patchingTextBox", j.errorText)
        clearInterval(i)
        patchInProgress = false
        UpdateUI()
    } else {
        TextBoxText("patchingTextBox", j.progressString + " - " + j.currentOperation)
    }
}

UpdateModsManually()
function UpdateModsManually() {
    fetch("/api/mods/mods").then(res => res.json().then(res => {
        modStatus = res
        UpdateMods()
    }))
}
var modStatus = {}
var modloader = "QuestLoader"

var lastCheckedAccessForGame = ""
function GetModLoaderName() {
    if(modloader == 0) return "QuestLoader"
    if(modloader == 1) return "Scotland2"
    return modloader
}

function RemoveOperation(id) {
    var o = modStatus.operations.find(x => x.operationId == id)
    for(const op of modStatus.operations) {
        if(o.taskId == op.taskId) DeleteOperationFromServer(op.operationId)
    }
    setTimeout(UpdateModsManually, 1500)
}

function DeleteOperationFromServer(id) {
    fetch(`/api/mods/operation`, {method: "DELETE", body: id})
}

function UpdateMods() {
    modStatus.operations = modStatus.operations.filter(x => !x.isDone || x.type == 6)
    operationsOngoing = modStatus.operations.length > 0
    var mods = ``
    if(!operationsOngoing) {
        operationsElement.style.display = "none"
    } else {
        operationsElement.style.display = "block"
        var operations = ""
        for(const operation of modStatus.operations){
            operations += `
                    <div class="mod" style="padding: 10px; ${operation.type == 6 ? "color: #FF0000;" : ""}">
                        ${operation.name}
                        ${operation.type == 6 ? `<div class="button" style="display: inline; margin-left: 20px;" onclick="RemoveOperation(${operation.operationId})">Remove Error</div>` : ""}
                    </div>
                    `
        }
        operationsList.innerHTML = operations
        ongoingCount.innerHTML = `Ongoing operations: ${modStatus.operations.length}`
    }
    for(const mod of modStatus.mods){
        mods += FormatMod(mod, !operationsOngoing)
    }
    if(mods == "") {
        mods = `<div class="mod" style="padding: 20px;">None installed</div>`
    }
    document.getElementById("modsList").innerHTML = mods
    var libs = ``
    for(const mod of modStatus.libs){
        libs += FormatMod(mod, !operationsOngoing)
    }
    if(libs == "") {
        libs = `<div class="mod" style="padding: 20px;">None installed</div>`
    }
    document.getElementById("libsList").innerHTML = libs
}
function IsOnQuest() {
    return location.host.startsWith("127.0.0.1") ||location.host.startsWith("localhost")
}

function GetPort() {
    return location.port
}

document.getElementById("cancellogin2").onclick = () => {
    CloseGetPasswordPopup()
}
var device = ""
fetch("/api/android/device").then(res => res.json().then(res => {
    if(res.sdkVersion <= 29) {
        // Android 10 and below don't need new storage perms
        document.getElementById("requestAppManage").style.display = "none"
        document.getElementById("requestAppData").style.display = "none"
    }
    if(res.device == "monterey" && IsOnQuest()) {
        // FUCK YOU QUEST 1 I HATE YOU SO BADLY RN JUST MAKE THAT FUQING FILE PICKIN CODE WORK ON Q1. IT WORKS ON LITERALLY STOCK ANDROID HGOUAEHGOAPEMJGHPUEL_GUHHUMÄ
        document.getElementById("installModButton").classList.add("notActive")
        document.getElementById("installModButton").style.display = "none"
        document.getElementById("installCosmeticButton").classList.add("notActive")
        document.getElementById("installCosmeticButton").style.display = "none"
        
        
        document.getElementById("installApkFromDisk").style.display = "none"
        document.getElementById("splashImageContainer").style.display = "none"
    }
    device = res.device
    UpdateInstalledModsQuest3Hint()
}))

function UpdateInstalledModsQuest3Hint() {
    var isOnQuest3 = device == "eureka" && modloader == 0 && !localStorage.installModsQ3 // only hide mods stuff on QuestLoader
    for(const e of document.getElementsByClassName("quest3hide")) {
        e.style.display = isOnQuest3 ? "none" : "block"
    }
    for(const e of document.getElementsByClassName("quest3show")) {
        e.style.display = isOnQuest3 ? "block" : "none"
    }
}
document.getElementById("installModsQ3").checked = localStorage.installModsQ3 == "true"

document.getElementById("installModsQ3").onchange = () => {
    localStorage.installModsQ3 = document.getElementById("installModsQ3").checked
    UpdateInstalledModsQuest3Hint()
}

function LaunchApp() {
    fetch("/api/android/launch", {
        method: "POST"
    });
}

function BrowserGo(direction) {
    browser.contentWindow.history.go(direction);
}

function OpenSite(url) {
    location = url
}



let toasts = 0;
let currentToasts = 0;
function ShowToast(msg, color, bgc) {
    toasts++;
    currentToasts++;
    let toastId = toasts;
    toastsE.innerHTML += `
    <div id="toast${toastId}" style="background-color: ${bgc}; color: ${color}; padding: 5px; height: 100px; width: 250px; position: fixed; bottom: ${(currentToasts - 1) * 120 + 20}px; right: 30px; border-radius: 10px">
        ${msg}
    </div>
    `;
    setTimeout(() => {
        document.getElementById(`toast${toastId}`).remove();
        currentToasts--;
    }, 5000)
}

if(!IsOnQuest()) {
    // Is not on quest
    document.getElementById("restoreBackup").classList.add("notActive")
    document.getElementById("onPcInfo").classList.remove("hidden")
    document.getElementById("uninstall").classList.add("notActive")
    document.getElementById("login").classList.add("notActive")
    document.getElementById("logout").classList.add("notActive")
    //document.getElementById("getModsButton").style.display = "none"
    
}

const cosmeticsTypeSelect = document.getElementById("cosmeticsType")
var cosmeticTypes = {}
function UpdateCosmeticsTypes() {
    fetch(`/api/cosmetics/types`).then(res => res.json().then(res => {
        cosmeticTypes = res
        if(res == null || res.fileTypes == null || res.fileTypes.length <= 0) {
            cosmeticsTypeSelect.innerHTML = `<option class="listItem" value="null">No types found</option>`;
            return;
        }
        var fvalue = cosmeticsTypeSelect.value
        var html = ""
        var htmlAvailableAfterModding = ""
        for(const value of res.fileTypes) {
            if(value.requiresModded && !isGamePatched) {
                if(htmlAvailableAfterModding == "") {
                    htmlAvailableAfterModding = `After patching the game you can add: `
                }
                htmlAvailableAfterModding += `${value.name}, `
            } else {
                if(!fvalue) {
                    fvalue = value.id
                }
                html += `<option class="listItem" value="${value.id}">${value.name} (${value.fileTypes.join(", ")})</option>`
            }
        }
        document.getElementById("availableAfterModdingTypes").style.display = htmlAvailableAfterModding != "" ? "block" : "none"
        document.getElementById("availableAfterModdingTypes").innerHTML = htmlAvailableAfterModding.substring(0, htmlAvailableAfterModding.length - 2)
        cosmeticsTypeSelect.innerHTML = html;
        if(html == "") {
            cosmeticsTypeSelect.innerHTML = `<option class="listItem" value="null">No types found</option>`;
        }
        if(!fvalue) fvalue = "null"
        cosmeticsTypeSelect.value = fvalue
        UpdateShownCosmetics()
    }))
}
UpdateCosmeticsTypes()

cosmeticsTypeSelect.onchange = () => UpdateShownCosmetics()

function UpdateShownCosmetics() {
    fetch(`/api/cosmetics/getinstalled?typeid=${cosmeticsTypeSelect.value}`).then(res => res.json().then(res => {
        var html = ``;
        if(res == null || res.length <= 0) {
            html = `<div class="listItem">None installed</div>`
        } else {
            for(const m of res) {
                html += FormatCosmetic(m)
            }
        }
        document.getElementById("cosmeticsList").innerHTML = html;
    }))
}

function FormatCosmetic(c) {
    return `
    <div class="mod" style="width: 100%;">
        <div style="display: flex; flex-direction: row; justify-content: space-between; width: 100%; margin: 20px;">
            <div>${c}</div>
            <div class="button" onclick="DeleteCosmetic('${c}')">Delete</div>
        </div>
    </div>
    `
}

function DeleteCosmetic(name) {
    fetch(`/api/cosmetics/delete?typeid=${cosmeticsTypeSelect.value}&filename=${name}`, {
        method: "DELETE"
    }).then(res => {
        UpdateShownCosmetics()
    })
}

document.getElementById("logintoken").onclick = () => {
    location = `?token=${document.getElementById("tokeninput").value}`
}

function UpdateVersion(version) {
    currentGameVersion = version
    document.getElementById("currentVersion").innerText = currentGameVersion ? currentGameVersion : "Version unknown"
}

var isGamePatched = false
const patchingOptions = document.getElementById("patchingOptions")
var patchingStatus = {}
var backupToPatch = ""
function ModBackup(backup) {
    backupToPatch = backup
    document.getElementById("selectedBackup").innerHTML = backup ? `Selected Backup: <b>${backup}</b><br><div class='button' onclick='ModBackup("")'>Unselect backup</div>` : ""
    OpenTab("patching")
    UpdatePatchingStatus()
    // do again to make sure it didn't get overriden by an earlier update which took longer
    setTimeout(UpdatePatchingStatus, 1000)
}
function UpdatePatchingStatus() {
    if(patchInProgress) {
        return;
    }
    patchStatus.innerHTML = `Loading<br><br>${squareLoader}`
    var backupExtra = "?package=" + config.currentApp + "&backup=" + backupToPatch
    fetch("/api/patching/getmodstatus" + (backupToPatch ? backupExtra : "")).then(res => {
        res.json().then(res => {
            UpdateVersion(res.version)
            patchingStatus = res
            isGamePatched = res.isPatched
            document.getElementById("copyOf").innerHTML = res.copyOf ? `Mods for <b>${res.copyOf}</b> can also be installed as this app is an copy of <b>${res.copyOf}</b>` : ""
            if (res.isPatched) {
                document.getElementById("modsButton").style.display = "block"
                patchingOptions.style.display = "none"
                patchStatus.innerHTML = "<h2>Game is already patched. You can install mods</h2>"
            } else if(!res.isInstalled) {
                patchStatus.innerHTML = `<h2>Game is not installed. Please restore a backup or install the app so the game can get patched</h2>`
                patchingOptions.style.display = "none"
                document.getElementById("modsButton").style.display = "none"
            } else if(res.canBePatched) {
                patchStatus.innerHTML = `<h2>Game is not patched.</h2>
                                        <div class="button" onclick="PatchGame()">Patch it now</div>`
                patchingOptions.style.display = "block"
                document.getElementById("modsButton").style.display = "none"
            } else {
                patchStatus.innerHTML = "<h2>Game can not be modded</h2>"
                patchingOptions.style.display = "none"
                document.getElementById("modsButton").style.display = "none"
            }
            UpdateCosmeticsTypes()
            if(!IsOnQuest() && !res.isPatched && false) {
                patchStatus.innerHTML = "<h2>To mod your game open QuestAppVersionSwitcher on your Quest</h2>"
                return;
            }
        })
    })
}
var operationsOngoing = false
const operationsElement = document.getElementById("operations")
const ongoingCount = document.getElementById("ongoingCount")
const operationsList = document.getElementById("operationsList")

const externalStorageCheckbox = document.getElementById("externalstorage")
const openXRCheckbox = document.getElementById("openxr")
const handTrackingCheckbox = document.getElementById("handtracking")
const handTrackingVersion = document.getElementById("handtrackingversion")
const debugCheckbox = document.getElementById("debug")
const otherContainer = document.getElementById("other")
var otherPermissions = []
var otherFeatures = []

function UploadMod() {
    var input = document.createElement('input');
    input.type = 'file';
    input.multiple = true;
    input.click();
    input.onchange = () => {
        if(input.files.length > 0) {
            for(const file of input.files) {
                fetch(`/api/install?filename=${file.name}`, {
                    method: "POST",
                    body: file
                }).then(res => {
                    UpdateShownCosmetics()
                })
            }
        }
    }
}

function InstallCosmetic() {
    var input = document.createElement('input');
    input.type = 'file';
    input.multiple = true;
    input.click();
    input.onchange = () => {
        if(input.files.length > 0) {
            for(const file of input.files) {
                // Only specify selected type if there is only one type with that extension. If the selected type is not the one with that extension, don't specify it
                var extra = `&typeid=${cosmeticsTypeSelect.value}`
                var extension = "." + file.name.split(".").pop()
                var extensionCount = 0
                var typeIsSelected = false
                for(const value of cosmeticTypes.fileTypes) {
                    if(value.fileTypes.map(x => x.toLowerCase()).includes(extension.toLowerCase())) {
                        extensionCount++
                        if(value.id == cosmeticsTypeSelect.value)typeIsSelected = true
                    }
                }
                if(extensionCount == 1 || !typeIsSelected) extra = ""
                fetch(`/api/install?filename=${file.name}${extra}`, {
                    method: "POST",
                    body: file
                }).then(res => {
                    UpdateShownCosmetics()
                })
            }
        }
    }
}

function DeleteMod(id) {
    return new Promise((resolve, reject) => {
        fetch(`/api/mods/delete?id=${id}`, {method: "POST"}).then(res => {
            resolve()
        }).catch(e => {
            reject(e)
        })
    });
}

function UpdateModState(id, enable) {
    fetch(`/api/mods/${enable ? `enable` : `uninstall`}?id=${id}`, {method: "POST"})
}

function FormatMod(mod, active = true) {
    return `
    <div class="mod">
        <div class="leftRightSplit">
            <img class="modCover" src="${mod.hasCover ? `/api/mods/cover/${mod.Id}` : `https://raw.githubusercontent.com/ComputerElite/ComputerElite.github.io/main/assets/ModCover.png`}">
            <div class="upDownSplit spaceBetween">
                <div class="upDownSplit">
                    <div class="leftRightSplit nomargin">
                        <div>${mod.Name}</div>
                        <div class="smallText version">v${mod.VersionString}</div>
                    </div>
                    <div class="smallText">${mod.Description}</div>
                </div>
                ${active ? `
                <div class="button" onclick="DeleteMod('${mod.Id}')">Delete</div>` : ``}
            </div>
        </div>
        <div class="upDownSplit spaceBetween relative">
            <div class="smallText margin20">
                (by ${mod.Author})
            </div>
            ${active ? `
            <label class="switch">
                <input onchange="UpdateModState('${mod.Id}', ${!mod.IsInstalled})" type="checkbox" ${mod.IsInstalled ? `checked` : ``}>
                <span class="slider round"></span>
            </label>` : ``}
            
        </div>
    </div>
    `
}

function AddPermission() {
    otherPermissions.push(document.getElementById("otherName").value)
    document.getElementById("otherName").value = ""
    UpdatePermissions()
}
function AddSpecificFeature(name, required) {
    otherFeatures.push({name: name, required: required})
    UpdateFeatures()
}

function AddFeature() {
    otherFeatures.push({name: document.getElementById("featureName").value, required: document.getElementById("requireFeature").checked})
    document.getElementById("featureName").value = ""
    UpdateFeatures()
}

function UpdateFeatures() {
    var perms = ""
    for(const p of otherFeatures){
        perms += `
        <div style="padding: 10px; margin-right: 20px; border-radius: 5px; border: 1px #242424 solid;">
            ${p.name} ${p.required ? "(Required)" : "(Not required)"}
            <div class="button" style="display: inline" onclick="RemoveFeature('${p.name}')">X</div>
        </div>`
    }
    if(perms == "") perms = "No custom features added yet"
    document.getElementById("otherFeatures").innerHTML = perms
}

function RemoveFeature(name) {
    otherFeatures = otherFeatures.filter(a => a.name != name)
    UpdateFeatures()
}

function UpdatePermissions() {
    var perms = ""
    for(const p of otherPermissions){
        perms += `
        <div style="padding: 10px; margin-right: 20px; border-radius: 5px; border: 1px #242424 solid;">
            ${p}
            <div class="button" style="display: inline" onclick="RemovePermission('${p}')">X</div>
        </div>`
    }
    if(perms == "") perms = "No custom permissions added yet"
    otherContainer.innerHTML = perms
}

function RemovePermission(name) {
    otherPermissions = otherPermissions.filter(a => a != name)
    UpdatePermissions()
}

var patchInProgress = false
var lastApp = ""
var scotlandForever
const packageRegex = /^[a-zA-Z]+(\.[a-zA-Z][a-zA-Z0-9_]*)+$/g

var splashBase64 = ""
function getBase64(file) {
    return new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.readAsDataURL(file);
        reader.onload = () => resolve(reader.result);
        reader.onerror = error => reject(error);
    });
}

async function AfterSplashSelect() {

    var file = document.getElementById("splashScreenInput").files[0];

    if (file.type != "image/png") {
        file = await ConvertImageToPNG(file);
    }
    
    getBase64(file).then(
        data => {
            SetSplashScreen(data)
        }
    )
}

function SetSplashScreen(data) {
    splashBase64 = data
    document.getElementById("splashImage").src = data
}

function ConvertImageToPNG(image) {
    return new Promise((resolve, reject) => {
        const img = new Image();
        img.src = URL.createObjectURL(image);
        img.onload = () => {
            const canvas = document.createElement("canvas");
            canvas.width = img.naturalWidth;
            canvas.height = img.naturalHeight;
            const ctx = canvas.getContext("2d");
            ctx.drawImage(img, 0, 0);
            canvas.toBlob((blob) => {
                if (!blob) {
                    reject("Failed to convert image to png");
                    return;
                }
                let fileName = image.name;
                if (!fileName.endsWith(".png")) {
                    fileName += ".png";
                }

                const file = new File([blob], fileName);

                resolve(file);
            }, "image/png");
        }
    })
}

document.getElementById("splashScreenInput").onchange = () => AfterSplashSelect()

function PatchGame() {
    patchInProgress = true
    var addMicPerm = document.getElementById("mic").checked
    if(addMicPerm) {
        otherPermissions = otherPermissions.filter(a => a != "android.permission.RECORD_AUDIO")
        otherPermissions.push("android.permission.RECORD_AUDIO")
    }
    var patchOptions = {
        otherPermissions: otherPermissions,
        otherFeatures: otherFeatures,
        debug: debugCheckbox.checked,
        handTracking: handTrackingCheckbox.checked,
        handTrackingVersion: parseInt(handTrackingVersion.value),
        externalStorage: externalStorageCheckbox.checked,
        openXR: openXRCheckbox.checked,
        modloader: parseInt(document.getElementById("modloader").value),
        resignOnly: document.getElementById("resignOnly").checked,
        customPackageId: document.getElementById("customPackageId").value,
        splashImageBase64: splashBase64
    }
    if(patchOptions.customPackageId && !packageRegex.test(patchOptions.customPackageId)) {
        TextBoxError("patchingTextBox", "invalid package id provided. Packackge id must match regex " + packageRegex)
        return;
    }
    if(patchOptions.modloader == 1 && document.getElementById("enableaudio").checked) {
        scotlandForever = new Audio("/scotlandforever.mp3")
        scotlandForever.addEventListener("canplaythrough", (event) => {
            /* the audio is now playable; play it if permissions allow */
            scotlandForever.play();
        });
    }
    var extra = backupToPatch ? `?package=${config.currentApp}&backup=${backupToPatch}` : ""
    fetch(`/api/patching/patchoptions`, {
        method: "POST",
        body: JSON.stringify(patchOptions)
    }).then(res => {
        fetch("/api/patching/patchapk" + extra, {
            method: "POST"
        }).then(res => {
            res.json().then(j => {
                if (j.success) {
                    TextBoxText("patchingTextBox", j.msg)
                    patchStatus.innerHTML = `<h2>Patching game<br><br>${squareLoader}</h2>`
                } else {
                    TextBoxError("patchingTextBox", j.msg)
                }
            })
        })
    })
}


UpdateUI()
TokenUIUpdate()
const oculusLink = "https://auth.oculus.com/login/?redirect_uri=https%3A%2F%2Fdeveloper.oculus.com%2Fmanage%2F"
const params = new URLSearchParams(window.location.search)
var afterRestore = ""
var afterDownload = ""

function CheckStartParams() {
    var download = params.get("download")
    var game = params.get("game")
    var version = params.get("version")
    var package = params.get("package")
    var modnow = params.get("modnow")
    var restorenow = params.get("restorenow")
    var backup = params.get("backup")
    var tab = params.get("tab")
    var logout = params.get("logout")
    var backuptopatchParam = params.get("backuptopatch")
    afterRestore = params.get("afterrestore")
    afterDownload = params.get("afterdownload")
    
    if(params.get("startlogin")) {
        StartLogin();
        return
    }
    if(params.get("showprocessing")) {
        OpenRestorePopup()
        GotoStep(17)
    }
    if(params.get("step16")) {
        OpenRestorePopup()
        GotoStep(16)
    }
    if(params.get("loginerror")) {
        OpenRestorePopup()
        GotoStep(18)
        document.getElementById("loginerror").innerText = params.get("loginerror")
    }
    
    if(params.get("loginsuccess")) {
        alert("logged in")
        location = location.href.split('?')[0]
        return
    }

    if(params.get("token")) {
        //OpenTokenPasswordPopup()
        // No password gets set by default anymore now
        fetch("/api/token", {
            method: "POST",
            body: JSON.stringify({
                token: params.get("token"),
                password: ""
            })
        }).then(res => {
            res.json().then(j => {
                if (j.success) {
                    alert("Logged in")
                    location = location.href.split('?')[0]
                } else {
                    alert("Error while logging in: " + j.msg)
                }
            })
        })
        return;
    }


    if(localStorage.redirect) {
        var loc  = localStorage.redirect + ""
        localStorage.redirect = ""
        window.open(loc, "_self")
        return
    }
    if(logout) {
        Logout()
    }
    if(tab) {
        OpenTab(tab)
    }
    
    if(restorenow) {
        RestoreBackup(backup, package)
    }
    
    if(backuptopatchParam) {
        ModBackup(backuptopatchParam)
    }

    if(params.get("restart")) {
        OpenGetPasswordPopup()
        GotoStep(10)
    }

    if(params.get("loadoculus")) {
        location = oculusLink
    }
    if(download) {
        if(diffDowngradeEnabled) {
            OpenTab("downgradeDiff")
        } else {
            fetch("/api/questappversionswitcher/loggedinstatus").then(res => {
                res.json().then(res => {
                    if(res.msg == "2") {
                        // Logged in
                        // Open downgrade tab
                        OpenTab("downgrade")
                        // Open OculusDB on correct page
                        document.getElementById("downgradeframe").src = `https://oculusdb.rui2015.me/id/${game}?downloadversion=${version}`
                    } else {
                        // Not logged in
                        OpenGetPasswordPopup()
                        GotoStep(13)
                    }
                })
            })
        }
    }

    if(modnow) {
        ChangeApp(package)
        OpenTab("patching")
        PatchGame()
    }
}

/*
document.getElementById("uninstallBS").onclick = () => {
    fetch("/api/android/uninstallpackage?package=com.beatgames.beatsaber&force=true", {method: "POST"})
}
 */

var config = {}
var selectedBackup = ""

var undefinedLoader = `<div class="lds-ellipsis"><div></div><div></div><div></div><div></div></div>`
setInterval(() => {
    TokenUIUpdate()
}, 5000)

function TokenUIUpdate() {
    fetch("/api/questappversionswitcher/loggedinstatus").then(res => {
        res.json().then(res => {
            if(res.msg == "2") {
                // Logged in
                document.getElementById("loggedInMsg").style.visibility = "visible"
                document.getElementById("downgradeLoginMsg").style.visibility = "hidden"
            } else {
                // Not logged in
                document.getElementById("loggedInMsg").style.visibility = "hidden"
                document.getElementById("downgradeLoginMsg").style.visibility = "visible"
            }
        })
    })
}

var firstConfigFetch = true;
var backupsFetching = false
function UpdateUI(closeLists = false) {
    UpdateShownCosmetics()
    fetch("/api/questappversionswitcher/config").then(res => res.json().then(res => {
        config = res
        if(firstConfigFetch) {
            firstConfigFetch = false;
            CheckStartParams()
            FetchModLoader()
        }
        Array.prototype.forEach.call(document.getElementsByClassName("packageName"), e => {
            if(config.currentApp) e.innerHTML = config.currentApp
            else e.innerHTML = "No app selected"
            if(config.currentApp != lastApp) UpdatePatchingStatus()
            lastApp = config.currentApp
        })
        if(config.currentApp) {
            if(!backupsFetching) {
                backupsFetching = true
                fetch("/api/backups?package=" + config.currentApp).then(res => res.json().then(res => {
                    backupsFetching = false
                    document.getElementById("backupList").innerHTML = ""
                    document.getElementById("size").innerHTML = res.backupsSizeString
                    if (res.backups) {
                        res.backups.forEach(backup => {
                            var extra = ""
                            if(backup.containsApk) {
                                if(backup.isPatchedApk) {
                                    extra = `<div style="margin-left: auto; margin-right: 0;">Modded</div>`
                                } else {
                                    extra = `<div class="button" style="margin-left: auto; margin-right: 0;" onClick="ModBackup('${backup.backupName}')">Mod this version</div>`
                                }
                            } else if(backup.containsAppData) {
                                extra = `<div style="margin-left: auto; margin-right: 0;">App data only</div>`
                            }
                            // Also show mod this version button if backup is not modded and has an apk
                            document.getElementById("backupList").innerHTML +=
                                `<div class="listItem${backup.backupName == selectedBackup ? " listItemSelected" : ""}" value="${backup.backupName}">${backup.isCorrupted ? `<b style="color: #FF0000">CORRUPTED </b>` : ``} ${backup.backupName} (${backup.backupSizeString}) ${extra}</div>`
                        })
                    }
                    if (document.getElementById("backupList").innerHTML == "") document.getElementById("backupList").innerHTML = `<div class="listItem" value="">No Backups</div>`
                    Array.prototype.forEach.call(document.getElementsByClassName("listItem"), i => {
                        i.onclick = function () {
                            selectedBackup = i.getAttribute("value")
                            UpdateUI()
                        }
                    })
                }))
            }
            UpdatePatchingStatus()
        } else {
            document.getElementById("backupList").innerHTML = `<div class="listItem" value="">Select an app via the change app button above</div>`
        }
    }))
    fetch("/api/allbackups").then(res => {
        res.json().then(j => {
            Array.prototype.forEach.call(document.getElementsByClassName("totalSize"), i => {
                i.innerHTML = j.msg
            })
        })
    })
    fetch("/api/questappversionswitcher/about").then(res => res.json().then(res => {
        document.getElementById("version").innerHTML = res.version
        document.getElementById("ips").innerHTML = ""
        res.browserIPs.forEach(i => {
            document.getElementById("ips").innerHTML += "<div style='margin-top: 15px;'><code>" + i + "</code></div>"
        })
    }))
    Array.prototype.forEach.call(document.getElementsByClassName("selectedBackupName"), i => {
        i.innerHTML = selectedBackup
    })
    if (closeLists) {
        document.getElementById("appList").className = "list hidden"
        document.getElementById("appListContainer").className = "listContainer hidden"
        document.getElementById("appList").innerHTML = ""
    }
}

function InstallAPKFromDisk() {
    fetch("/api/android/installapkfromdisk", {method: "POST"})
}

document.getElementById("login").onclick = () => {
    if(!IsOnQuest()) return;
    OpenGetPasswordPopup()
    GotoStep(9)
}

setInterval(() => {
    UpdateUI()
}, 10000)
function FormatDownload(d) {
    return `<div class="downloadContainer">
                    <div class="downloadProgressContainer">
                        <div class="downloadProgressBar" style="width: ${d.percentage * 100}%;"></div>
                    </div>
                    ${d.isCancelable ? `<input type="button" class="DownloadText" value="Cancel" onclick="StopDownload('${d.backupName}')">` : ``}
                    <div class="DownloadText" style="color: ${d.textColor};">
                        <b>${d.text}</b> ${d.percentageString} ${d.doneString} / ${d.totalString} ${d.speedString} ETA ${d.eTAString}
                    </div>
                </div>`
}
function UpdateDownloads(json) {
    var m = ""
    var gdms = ""
    for(const d of json.individualDownloads) {
        m += FormatDownload(d)
    }
    for(const d of json.gameDownloads) {
        var downloads = ""
        for(const download of d.downloadManagers) {
            downloads += FormatDownload(download)
        }
        gdms += `<div style="display: flex; flex-direction: column; background-color: #1F1F1F; padding: 10px;"><div class="downloadContainer">
                 ${!d.done ? (d.canceled || d.error ? `` : `<input type="button" class="DownloadText" style="width: 200px; background-color: #333333; font-size: 1.2em;" value="Cancel" onclick="StopGameDownload('${d.id}')">`) : `<input type="button" class="DownloadText" style="width: 200px; background-color: #333333; color: #00FF00; font-size: 1.2em;" value="Install Version" onclick="RestoreBackup('${d.backupName}', '${d.packageName}')">
<input type="button" class="DownloadText" style="width: 200px; background-color: #333333; color: #00FF00; font-size: 1.2em;" value="Mod Version" onclick="ModBackup('${d.backupName}')">`}
                <div class="DownloadText" style="color: ${d.textColor};">
                    <b>${d.canceled ? "Cancelled " : ""}${d.status}</b><br>${d.progressString} ${d.downloadedBytesString} / ${d.totalBytesString} ${d.speedString} ETA ${d.eTAString}
                </div>
            </div>
            ${downloads}
            </div>`
    }
    if (m == "") m = "<h2>No downloads running</h2>"
    if (gdms == "") gdms = "<h2>No game downloads running</h2>"
    document.getElementById("progressBarContainers").innerHTML = m
    document.getElementById("gameProgressBarContainers").innerHTML = gdms
}

document.getElementById("adb").onclick =()=> {
    location = "/adb"
}

document.getElementById("setup").onclick = () => {
    location = "/setup?open=true"
}

function StopGameDownload(id) {
    fetch("/api/cancelgamedownload?id=" + id, {method: "POST"})
}

function ShowAppList() {
    document.getElementById("appList").innerHTML = undefinedLoader
    document.getElementById("appList").className = "list"
    document.getElementById("appListContainer").className = "listContainer"
    fetch("hiddenApps.json").then(res => res.json().then(hiddenApps => {
        hiddenApps = hiddenApps.map(x => x.PackageName)
        fetch("/api/android/installedappsandbackups").then(res => res.json().then(res => {
            document.getElementById("appList").innerHTML = ""
            res.sort((a, b) => a.PackageName.localeCompare(b.PackageName))
            const interval = 10
            var target = 0
            var current = 0
            var i = setInterval(() => {
                target = res.length < target + interval ? res.length : target + interval
                var apps = ""
                while (current < target) {
                    var app = res[current]
                    if(hiddenApps.includes(app.PackageName)) {
                        current++
                        continue;
                    }
                    //apps += `<div onclick="ChangeApp('${app.PackageName}')" class="listItem${app.PackageName == config.currentApp ? " listItemSelected" : ""}" value="${app.PackageName}">${app.AppName} - ${app.PackageName}</div>`
                    apps += `<div onclick="ChangeApp('${app.PackageName}')" class="listItem${app.PackageName == config.currentApp ? " listItemSelected" : ""}" value="${app.PackageName}">${app.PackageName}</div>`
                    current++
                }
                document.getElementById("appList").innerHTML += apps
                if (current == res.length) clearInterval(i)
            }, 200)
        }))
    }))
    
}

function ChangeApp(package) {
    return new Promise((resolve, reject) => {
        console.log("Changing app to " + package)
        config.currentApp = package
        fetch("/api/questappversionswitcher/changeapp", {
            method: "POST",
            body: JSON.stringify({packageName: package})
        }).then(() => {
            resolve()
            UpdateUI(true)
            UpdateCosmeticsTypes()
            UpdateDowngrades()
        })
        UpdateUI(true)
    })
}

document.getElementById("exit").onclick = () => {
    fetch("/api/questappversionswitcher/kill", {method: "POST"})
}
document.getElementById("closeApp").onclick = () => {
    fetch("/api/questappversionswitcher/kill", {method: "POST"})
}
document.getElementById("confirmPort").onclick = () => {
    var port = parseInt(document.getElementById("port").value)
    if(port < 5000 || port > 6000) {
        TextBoxError("serverTextBox", "Only ports between 5000 and 6000 are allowed")
        return
    }
    fetch("/api/questappversionswitcher/changeport", {
        method: "POST",
        body: document.getElementById("port").value
    }).then(res => {
        res.json().then(j => {
            if(res.status == 200) {
                TextBoxGood("serverTextBox", j.msg)
            } else {
                TextBoxError("serverTextBox", j.msg)
            }
        })
    })
}

document.getElementById("appListContainer").onclick = (event) => {
    if (event.target.id == 'appListContainer') UpdateUI(true)
}
setInterval(() => {
    Array.prototype.forEach.call(document.getElementsByClassName("menuItem"), i => {
        i.onclick = function () {
            OpenTab(this.getAttribute("section"))
        }
    })
}, 500)

if(localStorage.lastOpened) OpenTab(localStorage.lastOpened)
function OpenTab(section) {
    Array.prototype.forEach.call(document.getElementsByClassName("menuItem"), e => {
        e.className = "menuItem" + (e.getAttribute("section") == section ? " selected" : "")
    })
    Array.prototype.forEach.call(document.getElementsByClassName("contentItem"), e => {
        e.className = "contentItem" + (e.id == section ? "" : " hidden")
    })
    localStorage.lastOpened = section
}


var backupInProgress = false

document.getElementById("confirmAccessBackup").onclick = () => {
    CloseRestorePopup()
    CreateBackupButton()
}
document.getElementById("createBackup").onclick = () => {
    CreateBackupButton()
}

function CreateBackupButton() {
    if (!config.currentApp) {
        TextBoxError("backupTextBox", "Please select an app first via the change app button above")
        return
    }
    if (backupInProgress) {
        TextBoxError("backupTextBox", "A Backup is being created right now. Please wait until it has finished")
        return
    }
    TextBoxText("backupTextBox", "Checking permission, one sec")
    fetch("/api/gotaccess?package=" + config.currentApp).then(res => {
        res.json().then(j => {
            if (j.gotAccess) {
                RealBackup()
            } else {
                TextBoxError("backupTextBox", "No permission was granted when you pressed the create backup button. Please try again.")
                CloseGetPasswordPopup();
                OpenRestorePopup();
                GotoStep("15")
            }
        })
    })
}

function RealBackup() {
    var onlyAppData = document.getElementById("appdata").checked
    backupInProgress = true
    TextBoxText("backupTextBox", "Please wait while the Backup is being created. This can take a few minutes")
    fetch("/api/backup?package=" + config.currentApp + "&backupname=" + document.getElementById("backupname").value + (onlyAppData ? "&onlyappdata=true" : ""), {
        method: "POST"
    }).then(res => {
        res.json().then(j => {
            if (res.status == 202) {
                TextBoxText("backupTextBox", j.msg)
                var i = setInterval(() => {
                    fetch("/api/backupstatus").then(res => {
                        res.json().then(j => {
                            if (j.done) {
                                TextBoxGood("backupTextBox", j.currentOperation)
                                clearInterval(i)
                                backupInProgress = false
                                UpdateUI()
                            } else if (j.error) {
                                TextBoxError("backupTextBox", j.errorText)
                                clearInterval(i)
                                backupInProgress = false
                                UpdateUI()
                            } else {
                                TextBoxText("backupTextBox", j.progressString + " - " + j.currentOperation)
                            }
                        })
                    })
                }, 500);
            } else {
                TextBoxError("backupTextBox", j.msg)
                backupInProgress = false
            }
            UpdateUI()
        })


    })
}

document.getElementById("changeApp").onclick = () => ShowAppList()
document.getElementById("changeApp2").onclick = () => ShowAppList()
document.getElementById("changeApp3").onclick = () => ShowAppList()

document.getElementById("abort").onclick = () => CloseRestorePopup()
document.getElementById("abort3").onclick = () => CloseRestorePopup()

document.getElementById("abort2").onclick = () => {
    CloseRestorePopup()
    if(afterRestore) location = decodeURIComponent(afterRestore)
}

function CloseRestorePopup() {
    document.getElementById("restoreContainer").className = "listContainer darken hidden"
    HideTextBox("step1box")
    HideTextBox("step3box")
    HideTextBox("step4box")
    GotoStep(1)
}

function OpenRestorePopup() {
    CloseRestorePopup()
    document.getElementById("restoreContainer").className = "listContainer darken"
}

document.getElementById("uninstall").onclick = () => {
    if(!IsOnQuest()) return
    fetch(`/api/android/uninstallpackage?package=${config.currentApp}`, {method: "POST"}).then(res => {
        if (res.status == 230) GotoStep(3)
        else GotoStep(2)
    })
}

document.getElementById("uninstall2").onclick = () => {
    fetch(`/api/backupinfo?package=${config.currentApp}&backupname=${selectedBackup}`).then(res => {
        res.json().then(j => {
            if(!j.containsApk) {
                AfterAPKInstall()
            } else {
                fetch(`/api/android/uninstallpackage?package=${config.currentApp}`, {method: "POST"}).then(res => {
                    if (res.status == 230) GotoStep(3)
                    else GotoStep(2)
                })
            }
        })
    })
    
}

document.getElementById("confirm1").onclick = () => {
    fetch("/api/android/ispackageinstalled?package=" + config.currentApp).then(res => {
        res.json().then(j => {
            if (!j.isAppInstalled) GotoStep(3)
            else {
                TextBoxError("step1box", config.currentApp + " is still installed. Please uninstall it.")
                GotoStep(1)
            }
        })
    })
}

document.getElementById("install").onclick = () => {
    fetch("/api/restoreapp?package=" + config.currentApp + "&backupname=" + selectedBackup, {
        method: "POST"
    }).then(res => {
        res.json().then(j => {
            backupToPatch = ""
            if (res.status == 200) {
                AfterAPKInstall()
            }
            else TextBoxError("step4.1box", j.msg)
        })
    })
}

function AfterAPKInstall() {
    fetch("/api/gotaccess?package=" + config.currentApp).then(res => {
        res.json().then(a => {
                fetch("/api/backupinfo?package=" + config.currentApp + "&backupname=" + selectedBackup).then(res => {
                    res.json().then(j => {
                        // 4.1: grant access
                        // 4.2: grant manage access
                        // 4: restore app data
                        // 5: done'd
                        if(j.isPatchedApk) {
                            if(j.moddedJson && j.moddedJson.modloaderName == "QuestLoader" && !a.gotAccess) {
                                GotoStep("4.1") // QuestLoader needs access to Android/data
                            } else {
                                GotoStep("4.2") // No need to ask for access
                            }
                        } else {
                            if (j.containsAppData || j.containsObbs) {
                                if(a.gotAccess) {
                                    GotoStep(4)
                                } else {
                                    GotoStep("4.1")
                                }
                            } else {
                                GotoStep(5)
                            }
                        }
                    })
                })
        })
    })
}

document.getElementById("grantManageAccess").onclick = () => {
    fetch("/api/android/ispackageinstalled?package=" + config.currentApp).then(res => {
        res.json().then(j => {
            if (j.isAppInstalled) {
                fetch("/api/grantmanagestorageappaccess?package=" + config.currentApp, {method: "POST"}).then(res => {
                    res.json().then(j => {
                        if (j.success) {
                            fetch("/api/backupinfo?package=" + config.currentApp + "&backupname=" + selectedBackup).then(res => {
                                res.json().then(j => {
                                    if (j.containsAppData || j.containsObbs) {
                                        GotoStep(4)
                                    } else {
                                        GotoStep(5)
                                    }
                                })
                            })
                        } else TextBoxError("step3box", j.msg)
                    })
                })
            }
            else {
                TextBoxError("step3box", config.currentApp + " is not installed. Please try again. Disable library sharing and remove all account from your quest except your primary one.")
                GotoStep(3)
            }
        })
    })
}

document.getElementById("grantAccess").onclick = () => {
    fetch("/api/android/ispackageinstalled?package=" + config.currentApp).then(res => {
        res.json().then(j => {
            if (j.isAppInstalled) {
                fetch("/api/grantaccess?package=" + config.currentApp, {method: "POST"}).then(res => {
                    res.json().then(j => {
                        if (j.success) {
                            fetch("/api/backupinfo?package=" + config.currentApp + "&backupname=" + selectedBackup).then(res => {
                                res.json().then(j => {
                                    if(j.isPatchedApk) {
                                        GotoStep("4.2")
                                    } else {
                                        if (j.containsAppData || j.containsObbs) {
                                            GotoStep(4)
                                        } else {
                                            GotoStep(5)
                                        }
                                    }
                                })
                            })
                        } else TextBoxError("step3box", j.msg)
                    })
                })
            }
            else {
                TextBoxError("step3box", config.currentApp + " is not installed. Please try again. Disable library sharing and remove all account from your quest except your primary one.")
                GotoStep(3)
            }
        })
    })
}

document.getElementById("deleteAllMods").onclick = () => {
    TextBoxText("updateTextBox", "Deleting all mods...")
    fetch("/api/mods/deleteallmods", {
        method: "POST"
    }).then(res => {
        res.json().then(j => {
            if(j.success) {
                TextBoxGood("updateTextBox", j.msg)
                setTimeout(() => {
                    // Refresh mods
                    ChangeApp(config.currentApp)
                }, 1000)
            } else {
                TextBoxError("updateTextBox", j.msg)
            }
        })
    });
}

document.getElementById("grantAccess2").onclick = () => {
    location = "/adb"
}

document.getElementById("grantAccess3").onclick = () => {
    location = `/adb?after=${encodeURIComponent(start + `/?step16=true`)}`
}

document.getElementById("requestManageStorageAppPermission").onclick = () => {
    fetch("/api/grantmanagestorageappaccess?package=" + config.currentApp, {method: "POST"}).then(res => {
    })
}

document.getElementById("restoreappdata").onclick = () => {
    fetch("/api/android/ispackageinstalled?package=" + config.currentApp).then(res => {
        res.json().then(j => {
            if (j.isAppInstalled) {
                TextBoxText("step4box", "Restoring game data. Please wait")
                fetch("/api/restoregamedata?package=" + config.currentApp + "&backupname=" + selectedBackup, {method: "POST"}).then(res => {
                    res.json().then(j => {
                        if (j.success) GotoStep(6)
                        else {
                            TextBoxError("step4box", j.msg)
                        }
                    })
                }).catch(err => {
                    GotoStep(6)
                })
            }
            else {
                TextBoxError("step3box", config.currentApp + " is not installed. Please try again. Disable library sharing and remove all account from your quest except your primary one.")
                GotoStep(3)
            }
        })
    })
}

document.getElementById("skip").onclick = () => {
    fetch("/api/android/ispackageinstalled?package=" + config.currentApp).then(res => {
        res.json().then(j => {
            if (j.isAppInstalled) GotoStep(6)
            else {
                TextBoxError("step3box", config.currentApp + " is not installed. Please try again. Disable library sharing and remove all account from your quest except your primary one.")
                GotoStep(3)
            }
        })
    })
}

document.getElementById("skip2").onclick = () => {
    TextBoxText("step3box", "checking... please wait")
    fetch("/api/android/ispackageinstalled?package=" + config.currentApp).then(res => {
        res.json().then(j => {
            if (j.isAppInstalled) GotoStep(6)
            else {
                TextBoxError("step3box", config.currentApp + " is not installed. Please install it.")
                GotoStep(3)
            }
        })
    })
}

document.getElementById("restoreBackup").onclick = () => {
    if(!IsOnQuest()) {
        TextBoxError("restoreTextBox", "You can only restore backups in your quest")
        return
    }
    if (backupInProgress) {
        TextBoxError("restoreTextBox", "Can not restore a backup while one is being created")
        return
    }
    if (selectedBackup == "") {
        TextBoxError("restoreTextBox", "You have to select a Backup from the list above")
        return
    }
    OpenRestorePopup()
    if(document.getElementById("restoreAppData").checked) GotoStep("4.1")
}

function GotoStep(step) {
    Array.prototype.forEach.call(document.getElementsByClassName("restoreStep"), e => {
        e.className = `restoreStep${e.id == "step" + step ? "" : " hidden"}`
    })
}

function CloseDeletePopup() {
    document.getElementById("deleteContainer").className = "listContainer darken hidden"
    GotoStep(6)
}
document.getElementById("deleteBackup").onclick = () => OpenDeletePopup();
function OpenDeletePopup() {
    CloseDeletePopup()
    document.getElementById("deleteContainer").className = "listContainer darken"
}

function CloseTokenPasswordPopup(done = false) {
    document.getElementById("getPasswordContainer").className = "listContainer darken hidden"
    GotoStep(8)
    if (!done) CloseTokenPasswordPopup(true)
}

function OpenTokenPasswordPopup() {
    CloseTokenPasswordPopup()
    document.getElementById("getPasswordContainer").className = "listContainer darken"
}

function CloseGetPasswordPopup() {
    document.getElementById("getPasswordContainer").className = "listContainer darken hidden"
    HideTextBox("step7box")
    GotoStep(7)
}

CheckUpdate()

document.getElementById("checkUpdate").onclick = () => CheckUpdate()
var updateAvailable = false
function CheckUpdate() {
    TextBoxText("updateTextBox", "Checking for updates...")
    fetch("/api/questappversionswitcher/checkupdate").then(res => res.json().then(json => {
        if(json.isUpdateAvailable) {
            updatePromptOpen = true
            CloseRestorePopup()
            OpenGetPasswordPopup()
            GotoStep(11)
            document.getElementById("updateAvailableText").innerHTML = json.msg + "<br><br>" + json.changelog.replace(/\n/g, "<br>")
        } else {
            TextBoxText("updateTextBox", json.msg)
        }
    }))
}

document.getElementById("cancelupdate").onclick = () => {
    CloseGetPasswordPopup()
    updateAvailable = false;
}

document.getElementById("updateqavs").onclick = () => {
    fetch("/api/questappversionswitcher/update", {method: "POST"}).then(res => res.json().then(j => {
        TextBoxText("step11box", j.msg)
    }))
}

function OpenGetPasswordPopup() {
    CloseGetPasswordPopup()
    document.getElementById("getPasswordContainer").className = "listContainer darken"
}
var options = {}
window.onmessage = (e) => {
    options = JSON.parse(e.data)
    if(!config.passwordSet) {
        CloseGetPasswordPopup()
        document.getElementById("getPasswordContainer").className = "listContainer darken"
        GotoStep(14)
        TextBoxText("step14box", "Waiting for response and requesting obbs to download from Oculus. This may take 30 seconds...")
        options.password = ""
        options.app = options.parentName
        document.getElementById("downloadStartingClosePopup").style.display = "none"
        fetch("/api/download", {
            method: "POST",
            body: JSON.stringify(options)
        }).then(res => {
            res.json().then(j => {
                if (!j.success) {
                    TextBoxError("step14box", j.msg)
                    document.getElementById("downloadStartingClosePopup").style.display = "block"
                } else {
                    TextBoxGood("step14box", j.msg)
                    document.getElementById("downloadStartingClosePopup").style.display = "block"
                    OpenTab("download")
                    if(afterDownload) location = decodeURIComponent(afterDownload)
                }
            })
        })
    } else {
        OpenGetPasswordPopup()
    }
}
document.getElementById("abortPassword").onclick = () => {
    document.getElementById("abortPassword").innerHTML = "Abort Download"
    document.getElementById("confirmPassword").style.display = "block"
    CloseGetPasswordPopup()
}

document.getElementById("downloadStartingClosePopup").onclick = () => {
    CloseGetPasswordPopup()
}

document.getElementById("logout").onclick = () => {
    if(!IsOnQuest()) return;
    Logout()
}

function Logout() {
    if(!IsOnQuest()) {
        alert("Cannot log out when on PC. Open QAVS in your quest")
        return;
    }
    fetch("/api/logout", {
        method: "POST"
    }).then(res => {
        alert("Logged out")
        window.open(location.href.split('?')[0])
    })
}

function RestoreBackup(backupName, game) {
    if(game && config.currentApp != game) {
        ChangeApp(game)
        setTimeout(() => RestoreBackupFromSelectedGame(backupName), 200)
        return;
    }
    RestoreBackupFromSelectedGame(backupName)
}

function RestoreBackupFromSelectedGame(backupName) {
    selectedBackup = backupName;
    OpenRestorePopup();
}
function PasswordInput() {
    TextBoxText("step7box", "Waiting for response and requesting obbs to download from Oculus. This may take 30 seconds...")
    options.password = encodeURIComponent(document.getElementById("passwordConfirm").value)
    options.app = options.parentName
    fetch("/api/download", {
        method: "POST",
        body: JSON.stringify(options)
    }).then(res => {
        res.json().then(j => {
            if (!j.success) {
                TextBoxError("step7box", j.msg)
            } else {
                TextBoxGood("step7box", j.msg)
                document.getElementById("abortPassword").innerHTML = "Close Popup"
                document.getElementById("confirmPassword").style.display = "none"
                OpenTab("download")
                if(afterDownload) location = decodeURIComponent(afterDownload)
            }
        })
    })
}
document.getElementById("confirmPassword").onclick = () => PasswordInput()

function StopDownload(name) {
    fetch(`/api/canceldownload?name=${name}`, {method: "POST"})
}

document.getElementById("logs").onclick = () => {
    TextBoxText("logsText", "Collecting information.. Please allow us 40 seconds to collect and upload everything")
    fetch("/api/questappversionswitcher/uploadlogs", {
        method: "POST",
        body: ""
    }).then(res => {
        res.json().then(j => {
            if (!j.success) {
                TextBoxError("logsText", j.msg)
            } else {
                TextBoxGood("logsText", "Logs uploaded successfully. Tell your support member this ID: " + j.msg)
            }
        })
    })
}

document.getElementById("abortLogin").onclick = () => {
    CloseGetPasswordPopup()
}
document.getElementById("confirmLogin").onclick = () => {
    TextBoxGood("step9box", "One sec...")
    StartLogin();
}
function StartLogin() {
    fetch("/api/login/start", {
        method: "POST"
    }).then(res => res.json().then(res => {
        window.open(res.url)
    }))
}

document.getElementById("login2").onclick = () => {
    TextBoxGood("step13box", "One sec...")
    StartLogin();
}
document.getElementById("abortLogin").onclick = () => {
    CloseGetPasswordPopup()
}

document.getElementById("tokenPassword").onclick = () => {
    options.password = encodeURIComponent(document.getElementById("passwordConfirm").value)
    options.app = options.parentName
    fetch("/api/token", {
        method: "POST",
        body: JSON.stringify({
            token: params.get("token"),
            password: encodeURIComponent(document.getElementById("passwordToken").value)
        })
    }).then(res => {
        res.json().then(j => {
            if (j.success) {
                TextBoxGood("step8box", j.msg + "<br>This pop up will close automatically in 5 seconds")
                setTimeout(() => {
                    TokenUIUpdate()
                    location = location.href.split('?')[0]
                }, 5000)
            } else {
                TextBoxError("step8box", j.msg + "<br>This pop up will close automatically in 10 seconds")
                setTimeout(() => {
                    location = location.href.split('?')[0]
                }, 10000)
            }
        })
    })
}

document.getElementById("reloadMods").onclick = () => {
    ChangeApp(config.currentApp)
    TextBoxGood("updateTextBox", "Reloaded mods")
}

document.getElementById("delete").onclick = () => {
    CloseDeletePopup()
    TextBoxText("restoreTextBox", "Deleting Backup. Please wait.")
    fetch("/api/backup?package=" + config.currentApp + "&backupname=" + selectedBackup, {
        method: "DELETE"
    }).then(res => {
        res.json().then(j => {
            if (j.success) {
                TextBoxGood("restoreTextBox", j.msg)
                UpdateUI()
            } else {
                TextBoxError("restoreTextBox", j.msg)
                UpdateUI()
            }
        })
    })
}

document.getElementById("abortDelete").onclick = () => {
    CloseDeletePopup()
}



function TextBoxError(id, text) {
    ChangeTextBoxProperty(id, "#EE0000", text)
}

function TextBoxText(id, text) {
    ChangeTextBoxProperty(id, "#03cffc", text)
}

function TextBoxGood(id, text) {
    ChangeTextBoxProperty(id, "#00EE00", text)
}

function HideTextBox(id) {
    document.getElementById(id).style.visibility = "hidden"
}

function ChangeTextBoxProperty(id, color, innerHtml) {
    var text = document.getElementById(id)
    text.style.visibility = "visible"
    text.style.border = color + " 1px solid"
    text.innerHTML = innerHtml
}
