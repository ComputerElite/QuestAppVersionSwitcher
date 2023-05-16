    const escapeHTMLPolicy = trustedTypes.createPolicy("myEscapePolicy", {
        createHTML: (string) => string.replace(/>/g, "<"),
    });

    var qavsInjectionDiv = document.createElement("div");
    document.body.appendChild(qavsInjectionDiv);
    const qavsPort = "{0}"

// Create nav bar
    var qavsNavbar = document.createElement("div");
    qavsNavbar.style = "color: #EEEEEE; position: fixed; top: 10px; right: 10px; background-color: #414141; border-radius: 5px; padding: 5px; display: flex; z-index: 50000;";

    var qavsBackButton = document.createElement("div");
    qavsBackButton.style = "border-radius: 5px; font-size: 100%; background-color: #5B5B5B; width: fit-content; height: fit-content; padding: 5px; cursor: pointer; flex-shrink: 0; user-select: none; margin-right: 5px;"
    qavsBackButton.onclick = () => {
        history.go(-1)
    }
    qavsBackButton.innerHTML = escapeHTMLPolicy.createHTML("Back")
    qavsNavbar.appendChild(qavsBackButton)
    
    var qavsForwardButton = document.createElement("div");
    qavsForwardButton.style = "border-radius: 5px; font-size: 100%; background-color: #5B5B5B; width: fit-content; height: fit-content; padding: 5px; cursor: pointer; flex-shrink: 0; user-select: none; margin-right: 5px;"
    qavsForwardButton.onclick = () => {
        history.go(1)
    }
    qavsForwardButton.innerHTML = escapeHTMLPolicy.createHTML("Forward")
    qavsNavbar.appendChild(qavsForwardButton)
    
    var qavsHomeButton = document.createElement("div");
qavsHomeButton.style = "border-radius: 5px; font-size: 100%; background-color: #5B5B5B; width: fit-content; height: fit-content; padding: 5px; cursor: pointer; flex-shrink: 0; user-select: none;"
    qavsHomeButton.onclick = () => {
        location = `http://127.0.0.1:${qavsPort}`
    }
    qavsHomeButton.innerHTML = escapeHTMLPolicy.createHTML("QuestAppVersionSwitcher")
    qavsNavbar.appendChild(qavsHomeButton)
    
    qavsInjectionDiv.appendChild(qavsNavbar)

// Handle popups
    var qavsPopupContainer = document.createElement("div");
    qavsPopupContainer.style = "font-size: 24px; border: 1px solid #4aaf8b; position: fixed; bottom: 10px; right: 10px; z-index: 50000; padding: 10px; border-radius: 5px; background-color: #414141; color: #EEEEEE; display: none;"
    qavsInjectionDiv.appendChild(qavsPopupContainer);

    const QAVSModInstall = 0
    const QAVSDependencyDownload = 4
    const QAVSModDownload = 7
    const QAVSQueuedModInstall = 8
    const QAVSError = 6

    var QAVSsomethingWasRunning = false;
    function UpdatePopUps() {
        fetch("http://localhost:" + qavsPort + "/api/mods/operations").then(res => {
            res.json().then(operations => {
                operations = operations.filter(x => !x.isDone && !x.isError);
                var queuedMods = operations.filter(x => x.type == QAVSQueuedModInstall);
                var installingMods = operations.filter(x => x.type == QAVSModInstall);
                var downloadingMods = operations.filter(x => x.type == QAVSModDownload);
                var downloadingDependencies = operations.filter(x => x.type == QAVSDependencyDownload);
                var errors = operations.filter(x => x.type == QAVSError);

                var html = `
                ${downloadingMods.length > 0 ? `<b>${downloadingMods.length}</b> mod(s) downloading<br>` : ``}
                ${queuedMods.length > 0 ? `<b>${queuedMods.length}</b> mod(s) queued for install<br>` : ``}
                ${installingMods.length > 0 ? `<b>${installingMods.length}</b> mod(s) installing<br>` : ``}
                ${downloadingDependencies.length > 0 ? `<b>${downloadingDependencies.length}</b> dependencies downloading<br>` : ``}
                ${errors.length > 0 ? `<div style="color: #EE0000;"><b>${errors.length}</b> error(s), more info in <a href="http://127.0.0.1:${qavsPort}?tab=mods">installed mods tab</a></div><br>` : ``}
            `;
                if(html.trim()) {
                    QAVSsomethingWasRunning = true
                } else {
                    html = "All done!"
                }
                qavsPopupContainer.innerHTML = escapeHTMLPolicy.createHTML(html);
                qavsPopupContainer.style.display = QAVSsomethingWasRunning ? "block" : "none";
            })
        })
    }
    setInterval(UpdatePopUps, 300);
    
    var qavsQueryparams = new URLSearchParams(location.search);
    

    if(location.href.startsWith("https://auth.meta.com/settings")) {
        // Open oculus store after logging in
        location = "https://oculus.com/experiences/quest"
    }
    if(location.host == "www.oculus.com") {
        console.log("oculus.com")
        if(qavsQueryparams.get("logout")) {
            console.log("logging out")
            setTimeout(() => {
                location = 'https://secure.oculus.com/logout/'
            }, 3000)
        } else {
            // Click login button
            setTimeout(() => {
                location = 'https://auth.oculus.com/login/?redirect_uri=https%3A%2F%2Fwww.oculus.com%2Fexperiences%2Fquest%2F'
            }, 3000)

            // Send token to qavs
            var ws = new WebSocket('ws://localhost:' + qavsPort + '/' + document.body.innerHTML.substr(document.body.innerHTML.indexOf("accessToken"), 200).split('"')[2]);
        }
    }

// Modify sign in options on auth.meta.com
    if(location.host.contains("auth.meta.com")) {
        console.log("auth.meta.com")
        if(location.href.startsWith("https://auth.meta.com/register/facebook/")) {
            // Registering with facebook shouldn't be happening on headset
            alert("You mustn't register a new meta account on your headset. Please use another method to log in")
            location = "https://auth.meta.com/"
        }
        setTimeout(() => {
            // Remove facebook button
            /*
            for(const e of document.getElementsByTagName("span")) {
                if(e.innerHTML.toLowerCase().contains("facebook") && e.innerHTML.toLowerCase().contains("continue with") && !e.innerHTML.toLowerCase().contains("span")) {
                    console.log(e.parentElement)
                    e.parentElement.parentElement.parentElement.parentElement.parentElement.parentElement.parentElement.style.display = "none"
                }
            }
            */
            // Remove instagram button
            /*
            for(const e of document.getElementsByTagName("span")) {
                if(e.innerHTML.toLowerCase().contains("instagram") && e.innerHTML.toLowerCase().contains("continue with") && !e.innerHTML.toLowerCase().contains("span")) {
                    console.log(e.parentElement)
                    e.parentElement.parentElement.parentElement.parentElement.parentElement.parentElement.parentElement.style.display = "none"
                }
            }
            */
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
                    e.innerHTML = escapeHTMLPolicy.createHTML("Log in with email (preferred method)")
                    e.parentElement.parentElement.parentElement.parentElement.style.backgroundColor = "#1B2930"
                    e.style.color = "#F2F2F2"
                    
                    var buttonContainer = e.parentElement.parentElement.parentElement.parentElement.parentElement.parentElement.parentElement
                    buttonContainer.parentElement.insertBefore(buttonContainer, buttonContainer.parentElement.children.item(3))
                }
            }
            // Remove set oculus button
            for(const e of document.getElementsByTagName("span")) {
                if(e.innerHTML.toLowerCase().contains("oculus") && e.innerHTML.toLowerCase().contains("span")) {
                    console.log(e.parentElement)
                    e.parentElement.style.display = "none"
                }
            }
        }, 1000)
    }
