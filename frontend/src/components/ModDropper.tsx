import { createSignal, onCleanup, onMount } from "solid-js";
import toast from "solid-toast";
import { Sleep } from "../util";
import { gotAccessToAppAndroidFolders, grantAccessToAppAndroidFolders } from "../api/android";
import { config, moddingStatus } from "../store";
import { showChangeGameModal } from "../modals/ChangeGameModal";
import { InstallModFromUrl, UploadMod } from "../api/mods";
import style from "./ModDropper.module.scss";

async function checkModsCanBeInstalled() {
    if (config()?.currentApp == null) {
        showChangeGameModal();
        toast.error("No game selected! Select a game first");
        return false;
    }
    
    if (!(moddingStatus()?.isInstalled ?? false)) {
        return toast.error("Game is not installed! Install it first before installing mods");
    }

    if (!(moddingStatus()?.isPatched ?? false)) {
        return toast.error("Game is not modded! Mod it first before installing mods");
    }

    let hasAccess = await gotAccessToAppAndroidFolders(config()!.currentApp);
    if (!hasAccess) {
        toast.error("Failed to get access to game folders. We will request access again in 3 seconds, try again after that.");
        Sleep(3000);
        let result = await grantAccessToAppAndroidFolders(config()!.currentApp);
        return false;
    }

    return true;
}

async function onFileDrop(e: DragEvent) {
    e.preventDefault();
    e.stopPropagation();


    // If dropped items aren't files, reject them
    if (!e.dataTransfer) return;

    // If it's files, process them and send them to the server one by one
    if (e.dataTransfer) {
        let filesToUpload: Array<File> = [];


        // Try 2 ways of getting files 
        if (e.dataTransfer.items) {
            // Use DataTransferItemList interface to access the file(s)
            [...e.dataTransfer.items].forEach((item, i) => {
                // If dropped items aren't files, reject them
                if (item.kind === 'file') {
                    const file = item.getAsFile();
                    if (file) {
                        console.log(`… file[${i}].name = ${file.name}`);
                        filesToUpload.push(file);
                    }
                }
            });
        } else {
            // Use DataTransfer interface to access the file(s)
            [...e.dataTransfer.files].forEach((file, i) => {
                filesToUpload.push(file);
            });
        }
        // Get the url if there is one
        let url = e.dataTransfer.getData("URL");

        // Check if we can install mods here because we will lose the drag event if we await
        if (!(await checkModsCanBeInstalled())) return;

        if (url) {
            await InstallModFromUrl(url);
            await Sleep(100);

        }

        if (filesToUpload.length > 0) {
            for (const file of filesToUpload)
                await UploadMod(file);
        }
    }

}

export function ModDropper () {
    const [isDragging, setIsDragging] = createSignal(false);
    const [dragCounter, setDragCounter] = createSignal(0);
    
    function ondragenter(e: DragEvent) {
        e.preventDefault();
        e.stopPropagation();
        if (dragCounter() + 1 >= 0) {
            setIsDragging(true);
        }
        setDragCounter(dragCounter() + 1);
    }
    function ondragleave(e: DragEvent) {
        e.preventDefault();
        e.stopPropagation();
        setDragCounter(dragCounter() - 1);
        if (dragCounter() <= 0) {
            setIsDragging(false);
        }
    }

    function ondragover(e: DragEvent) {
        e.preventDefault();
        e.stopPropagation();
    }

    function ondrop(e: DragEvent) {
        onFileDrop(e);
        setIsDragging(false);
    }

    onMount(async () => {
        window.addEventListener("drop", ondrop);
        window.addEventListener("dragover", ondragover);
        window.addEventListener("dragleave", ondragleave);
        window.addEventListener("dragenter", ondragenter);
        console.log("mounted")
    })

    onCleanup(() => {
        window.removeEventListener("drop", ondrop);
        window.removeEventListener("dragover", ondragover);
        window.removeEventListener("dragleave", ondragleave);
        window.removeEventListener("dragenter", ondragenter);
        console.log("unmounted")
    })

    return (
        <div classList={{
            [`${style.dragOverlay}`]: true,
            [`${style.active}`]: isDragging()
        }
        }>
            <div class={style.dragOverlayText}>Drop to install</div>
        </div>
    )

}