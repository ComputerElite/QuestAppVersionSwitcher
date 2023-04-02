import { Routes, Route, A } from "@solidjs/router";
import { For } from "solid-js";
import "./Sidebar.scss"

let gameInstalled = true;

let links = [
    { name: "Backup", href: "/backup" },
    { name: "Downgrade", href: "/downgrade" },
    { name: "Download progress", href: "/downloads" },
    { name: "Mod my game", href: "/patching", },
    { name: "Installed Mods", href: "/mods" },
    { name: "Cosmetics & more", href: "/cosmetics" },
    { name: "Get Mods & cosmetics", href: "/getMods" },
    { name: "Tools & Options", href: "/tools" },
]

export default function Sidebar() {


    return (
        <div class="sidebar">
            
            <div class="header">
                <div style="width: 100%;">Welcome to Quest App Version Switcher</div>
                <div style="font-size: 80%; width: 100%;">
                    Managing <div class="inline packageName">some game</div>
                </div>
            </div>
            
            <For each={links} >
                {(link) => (
                    <A href={link.href} class="menuItem" activeClass="selected" >{link.name}</A>
                )}
            </For>
        </div>
    )
}
