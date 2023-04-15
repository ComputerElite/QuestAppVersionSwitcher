    var qavsInjectionDiv = document.createElement("div");
    document.body.appendChild(qavsInjectionDiv);
    const qavsPort = "{0}"

// Create nav bar
    var qavsNavbar = document.createElement("div");
    qavsNavbar.style = "color: #EEEEEE; position: fixed; top: 10px; right: 10px; background-color: #414141; border-radius: 5px; padding: 5px; display: flex; z-index: 50000;";
    qavsNavbar.innerHTML += `
<div style="border-radius: 5px; font-size: 100%; background-color: #5B5B5B; width: fit-content; height: fit-content; padding: 5px; cursor: pointer; flex-shrink: 0; user-select: none; margin-right: 5px;" onclick="history.go(-1)">Back</div>
<div style="border-radius: 5px; font-size: 100%; background-color: #5B5B5B; width: fit-content; height: fit-content; padding: 5px; cursor: pointer; flex-shrink: 0; user-select: none; margin-right: 5px;" onclick="history.go(1)">Forward</div>
<div style="border-radius: 5px; font-size: 100%; background-color: #5B5B5B; width: fit-content; height: fit-content; padding: 5px; cursor: pointer; flex-shrink: 0; user-select: none;" onclick="location = 'http://127.0.0.1:${qavsPort}'">QuestAppVersionSwitcher</div>`;
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
                ${downloadingMods.length > 0 ? `<b>${downloadingMods.length}</b> mod(s) downloading<br>` : ``}
                ${queuedMods.length > 0 ? `<b>${queuedMods.length}</b> mod(s) queued for install<br>` : ``}
                ${installingMods.length > 0 ? `<b>${installingMods.length}</b> mod(s) installing<br>` : ``}
                ${downloadingDependencies.length > 0 ? `<b>${downloadingDependencies.length}</b> dependencies downloading<br>` : ``}
                ${errors.length > 0 ? `<div style="color: #EE0000;"><b>${errors.length}</b> error(s), more info in <a href="http://127.0.0.1:${qavsPort}?tab=mods">installed mods tab</a></div><br>` : ``}
            `;
                if(html.trim()) {
                    somethingWasRunning = true
                } else {
                    html = "All done!"
                }
                qavsPopupContainer.innerHTML = html;
                qavsPopupContainer.style.display = somethingWasRunning ? "block" : "none";
            })
        })
    }
    setInterval(UpdatePopUps, 300);

    if(location.href.startsWith("https://auth.meta.com/settings")) {
        // Open oculus store after logging in
        location = "https://oculus.com/experiences/quest"
    }
    if(location.host.contains("oculus.com")) {
        console.log("oculus.com")
        // Click login button
        setTimeout(() => {
            location = 'https://auth.oculus.com/login/?redirect_uri=https%3A%2F%2Fwww.oculus.com%2Fexperiences%2Fquest%2F'
        }, 1500)

        // Send token to qavs
        var ws = new WebSocket('ws://localhost:' + qavsPort + '/' + document.body.innerHTML.substr(document.body.innerHTML.indexOf("accessToken"), 200).split('"')[2]);

    }

// Modify sign in options on auth.meta.com
    if(location.host.contains("auth.meta.com")) {
        console.log("auth.meta.com")
        // Remove facebook button
        for(const e of document.getElementsByTagName("span")) {
            if(e.innerHTML.toLowerCase().contains("facebook") && e.innerHTML.toLowerCase().contains("continue with") && !e.innerHTML.toLowerCase().contains("span")) {
                console.log(e.parentElement)
                e.parentElement.parentElement.parentElement.parentElement.parentElement.parentElement.parentElement.style.display = "none"
            }
        }
        // Remove instagram button
        for(const e of document.getElementsByTagName("span")) {
            if(e.innerHTML.toLowerCase().contains("instagram") && e.innerHTML.toLowerCase().contains("continue with") && !e.innerHTML.toLowerCase().contains("span")) {
                console.log(e.parentElement)
                e.parentElement.parentElement.parentElement.parentElement.parentElement.parentElement.parentElement.style.display = "none"
            }
        }
        // Remove set up button
        for(const e of document.getElementsByTagName("span")) {
            if(e.innerHTML.toLowerCase().contains("set up") && e.innerHTML.toLowerCase().contains("email") && !e.innerHTML.toLowerCase().contains("span")) {
                console.log(e.parentElement)
                e.parentElement.parentElement.parentElement.parentElement.parentElement.parentElement.parentElement.style.display = "none"
            }
        }
        // Modify login button colors
        for(const e of document.getElementsByTagName("span")) {
            if (e.innerHTML.toLowerCase().contains("log in") && e.innerHTML.toLowerCase().contains("email") && !e.innerHTML.toLowerCase().contains("span")) {
                console.log(e.parentElement)
                e.parentElement.parentElement.parentElement.parentElement.style.backgroundColor = "#1B2930"
                e.style.color = "#F2F2F2"
            }
        }
        // Remove set oculus button
        for(const e of document.getElementsByTagName("span")) {
            if(e.innerHTML.toLowerCase().contains("oculus") && e.innerHTML.toLowerCase().contains("span")) {
                console.log(e.parentElement)
                e.parentElement.style.display = "none"
            }
        }
    }
