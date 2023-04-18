import toast from "solid-toast";

/**
 * Does the app contain only app data
 * @param appId 
 * @param backupName 
 * @returns 
 */
export async function isOnlyAppData(appId: string, backupName: string): Promise<boolean> {
    let params = new URLSearchParams();

    params.append("package", appId);
    params.append("backupname", backupName);

    let result = await fetch(`/api/isonlyappdata?${params}`);
    if (result.status == 200) {
        let text = await result.text();
        if (text == "true") {
            return true;
        }
    }

    if (result.status == 400) {
        let text = await result.text();
        throw new Error(text);
    }

    return false;
}

/**
 * Checks if the given package is installed
 * @param appId app id (com.beatgames.beatsaber)
 * @returns  true if the package is installed
 */
export async function isPackageInstalled(appId: string): Promise<boolean> {
    let result = await fetch(`/api/android/ispackageinstalled?package=${appId}`);
    if (result.ok) {
        let text: {
            isAppInstalled: boolean;
            msg: string;
            success: boolean;
        } = await result.json();

        if (text.success && text.isAppInstalled) {
            return true;
        }
    }
    return false;
}


export async function grantManageStorageAccess(appId: string): Promise<boolean> {
    let result = await fetch(`/api/grantmanagestorageappaccess?package=${appId}`);
    if (result.ok) {
        return true;
    }
    return false;
}

/**
 * Uninstall the given package
 * @param appId  app id (com.beatgames.beatsaber)
 * @returns true if the package is uninstalled
 */
export async function uninstallPackage(appId: string): Promise<boolean> {
    let result = await fetch(`/api/android/uninstallpackage?package=${appId}`);

    if (result.ok) {
        return true;
    }
    if (result.status == 230) {
        toast("App is already uninstalled");
        return true;
    }

    throw new Error(await result.text());
}

/**
 * Checks if QAVS has access to the android folders /sdcard/Android/data/ and /sdcard/Android/obb/
 * @param appId 
 * @returns true if the app has access
 */
export async function gotAccessToAppAndroidFolders(appId: string): Promise<boolean> {
    let result = await fetch(`/api/gotaccess?package=${appId}`);
    if (result.ok) {
        let text = await result.text();
        if (text.toLowerCase() == "true") {
            return true;
        }
    } else {
        if (result.status == 400) {
            let msg = "Failed to check storage access: package key needed";
            // This probably needs to be removed
            toast.error(msg);
            throw new Error(msg);
        }
    }
    return false;
}

/**
 * Authorize QAVS to access the android folders /sdcard/Android/data/ and /sdcard/Android/obb/
 * @param appId app id (com.beatgames.beatsaber)
 * @returns true if the app requested access
 */
export async function grantAccessToAppAndroidFolders(appId: string): Promise<boolean> {
    let result = await fetch(`/api/grantaccess?package=${appId}`);
    if (result.ok) {
        return true;
    } else {
        if (result.status == 400) {
            let msg = "Failed to check storage access: package key needed";
            toast.error(msg);
            throw new Error(msg);
        }
    }
    return false;
}

/**
 * Upload an apk file to backups
 * @param file apk file
 */
export async function uploadAPK(file: File) {
    let formData = new FormData();
    formData.append("file", file);

    let result = await fetch("/api/android/uploadandinstallapk", {
        method: "POST",
        body: formData
    });
}


/**
 * Install an apk file from the given path on the qavs server
 * @deprecated this api is blocked cause it's a security risk
 */
export async function installAPK(path: string) {
    let result = await fetch(`/api/android/installapk?path=${path}`);
    if (result.ok) {
        return true;
    }
    if (result.status == 503) {
        let msg = await result.text();
        throw new Error(msg);
    }
    if (result.status == 400) {
        let msg = await result.text();
        throw new Error(msg);
    }
    throw new Error("Unknown error");
}

/**
 * Launches currently selected app
 * @returns 
 */
export async function launchCurrentApp(): Promise<string> {
    let result = await fetch("/api/android/launch");
    let text = await result.text();
    return text;
}



interface getDeviceInfoResponse {
    sdkVersion: number;
}
/**
 * Gets the device info
 * @returns sdk version of the device (29 - android 10, 32 - android 12.1)
 */
export async function getDeviceInfo(): Promise<getDeviceInfoResponse> {
    let result = await fetch("/api/android/device");
    let json = await result.json();
    return json;
}
