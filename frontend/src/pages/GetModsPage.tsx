import { For, createMemo } from "solid-js"

import "./GetModsPage.scss";


export default function GetModsPage() {
    let currentGameVersion = 2;
    let sites = createMemo(() => {
        return [
            {
                name: "Beat Saber Mods",
                url: `https://computerelite.github.io/tools/Beat_Saber/questmods.html?version=${currentGameVersion}`
            },
            {
                name: "Google",
                url: `https://www.google.com/search?q=computerelite`
            }
        ]
    }, [currentGameVersion])



    return (
        <div class="contentItem getModsPage">
            <h2 class="contentHeader">
                Sites to get mods and cosmetics from
                
            </h2>
            <p>
                Only QMods are supported by QuestAppVersionSwitcher + any cosmetics formats of the currently selected game
            </p>
            <div class="buttonsContainer">
                <For each={sites()}>
                    {(site) => (
                        <a class="button labelMargin" href={site.url}>{site.name}</a>
                    )}
                </For>
            </div>
        </div>
    )
}