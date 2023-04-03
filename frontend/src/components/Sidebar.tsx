import { Routes, Route, A } from "@solidjs/router";
import { For } from "solid-js";
import "./Sidebar.scss"
import { config } from "../store";

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
                <div style="width: 100%; font-size: 1em;">Quest App Version Switcher</div>
                <div style="font-size: 70%; width: 100%;">
                     <div style="color: #F9F" title="Managed" class="inline packageName">{config()?.currentApp?? "some app"}</div>
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
