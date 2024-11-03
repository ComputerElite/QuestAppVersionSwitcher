import toast from "solid-toast";
import { config } from "../../store";
import { isPackageInstalled } from "../../api/android";
import { IsOnQuest } from "../../util";
import { restoreAppBackup } from "../../api/backups";

export async function checkIfGameIsInstalled() {
    let currentApplication = config()?.currentApp;
    if (!currentApplication) {
        toast.error("No game selected");
        return false;
    }
    let installationStatus = await isPackageInstalled(currentApplication);


    return installationStatus;

}


export async function installGame(name: string) {
    let currentGame = config()?.currentApp;

    if (!currentGame) return toast.error("No game selected! Open Change App modal and select a game.")

    if (!IsOnQuest()) {
        toast("Install dialog is open on quest itself!")
    }

    if (await isPackageInstalled(currentGame)) {
        return toast.error("Game is already installed, uninstall the game to install it again lol!")
    }

    await restoreAppBackup(currentGame, name);
}