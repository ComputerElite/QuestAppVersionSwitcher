var qavsInjectionDiv = document.createElement("div");
document.body.appendChild(qavsInjectionDiv);
const qavsPort = "{0}"

// Create nav bar
var qavsNavbar = document.createElement("div");
qavsNavbar.style = "color: #EEEEEE; position: fixed; top: 10px; right: 10px; background-color: #414141; border-radius: 5px; padding: 5px; display: flex; z-index: 50000;";
qavsNavbar.innerHTML += `
<div style="border-radius: 5px; font-size: 100%; background-color: #5B5B5B; width: fit-content; height: fit-content; padding: 5px; cursor: pointer; flex-shrink: 0; user-select: none; margin-right: 5px;" onclick="history.go(-1)">Back</div>
<div style="border-radius: 5px; font-size: 100%; background-color: #5B5B5B; width: fit-content; height: fit-content; padding: 5px; cursor: pointer; flex-shrink: 0; user-select: none; margin-right: 5px;" onclick="history.go(1)">Forward</div>
<div style="border-radius: 5px; font-size: 100%; background-color: #5B5B5B; width: fit-content; height: fit-content; padding: 5px; cursor: pointer; flex-shrink: 0; user-select: none; margin-right: 5px;" onclick="location = 'http://localhost:${qavsPort}'">QuestAppVersionSwitcher</div>
<div style="border-radius: 5px; font-size: 100%; background-color: #5B5B5B; width: fit-content; height: fit-content; padding: 5px; cursor: pointer; flex-shrink: 0; user-select: none;" onclick="location = 'https://oculus.com/experiences/quest'">Oculus (Login)</div>`;
qavsInjectionDiv.appendChild(qavsNavbar)

// Handle popups
var qavsPopupContainer = document.createElement("div");
qavsPopupContainer.style = "font-size: 24px; border: 1px solid #4aaf8b; position: fixed; bottom: 10px; right: 10px; z-index: 50000; padding: 10px; border-radius: 5px; background-color: #414141; color: #EEEEEE; display: none;"
qavsInjectionDiv.appendChild(qavsPopupContainer);

const ModInstall = 0
const DependencyDownload = 4
const ModDownload = 7
const QueuedModInstall = 8
const Error = 6

var somethingWasRunning = false;
function UpdatePopUps() {
    fetch("http://localhost:" + qavsPort + "/api/mods/operations").then(res => {
        res.json().then(operations => {
            operations = operations.filter(x => !x.isDone);
            var queuedMods = operations.filter(x => x.type == QueuedModInstall);
            var installingMods = operations.filter(x => x.type == ModInstall);
            var downloadingMods = operations.filter(x => x.type == ModDownload);
            var downloadingDependencies = operations.filter(x => x.type == DependencyDownload);
            var errors = operations.filter(x => x.type == Error);
            
            var html = `
                ${queuedMods.length > 0 ? `${queuedMods.length} mods queued<br>` : ``}
                ${installingMods.length > 0 ? `${installingMods.length} mods installing<br>` : ``}
                ${downloadingMods.length > 0 ? `${downloadingMods.length} mods downloading<br>` : ``}
                ${downloadingDependencies.length > 0 ? `${downloadingDependencies.length} dependencies downloading<br>` : ``}
                ${errors.length > 0 ? `${errors.length} errors, more info in installed mods tab<br>` : ``}
            `;
            if(!html) {
                html = "All done!"
            } else {
                somethingWasRunning = true
            }
            qavsPopupContainer.innerHTML = html;
            qavsPopupContainer.style.display = somethingWasRunning ? "block" : "none";
        })
    })
}
setInterval(UpdatePopUps, 300);