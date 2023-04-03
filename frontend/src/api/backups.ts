import toast from "solid-toast";

export interface IBackup {
    backupName: string;
    backupLocation: string;
    containsGamedata: boolean;
    backupSize: number;
    backupSizeString: string;
}


export interface getBackupsResponse {
    backups: Array<IBackup>;
    lastRestored: string;
    backupsSize: number;
    backupsSizeString: string;
}



/**
 * Gets the backups for the given app
 * @param appId com.beatgames.beatsaber
 * @returns
 * - If backup directory does not exist: {}
 * - If backup directory exists: {backups: Array<IBackup>, lastRestored: string, backupsSize: number, backupsSizeString: string}
 */
export async function getBackups(appId: string): Promise<getBackupsResponse | {}> {
    let result = await fetch(`/api/backups?package=${appId}`);
    if (result.ok) {
        return await result.json();
    }
    if (result.status == 400 || result.status == 500) {
        let text = await result.text();
        throw new Error(text);
    }
    throw new Error("Unknown error");
}

/**
 * Restore the given app from a backup, this will just request the quest to open dialog to install the app, response will be true if the request was successful
 * @param appId app id (com.beatgames.beatsaber) 
 * @param backupName backup name
 * @returns true if the request was successful
 * @throws Error if the request was not successful
 */
export async function restoreAppBackup(appId: string, backupName: string) {
    let result = await fetch(`/api/restoreapp?package=${appId}&backupname=${backupName}`);

    if (result.ok) {
        return true;
    }
    if (result.status == 400) {
        let text = await result.text();
        throw new Error(text);

    }
    if (result.status == 500) {
        let text = await result.text();
        throw new Error(text);
    }

    return await result.json();
}

export async function deleteBackup(appId: string, backupName: string) {
    let params = new URLSearchParams();
    params.append("package", appId);
    params.append("backupname", backupName);

    let result = await fetch(`/api/backup?${params}`, { method: "DELETE" });
    return await result.json();
}

/**
 * Creates a backup for the given app
 * @param appId app id (com.beatgames.beatsaber)
 * @param backupName any name
 * @param onlyAppData if true, only the app data will be backed up
 * @returns
 * - if the backup is in progress, it will return the message from the server,
 * - if the backup is not in progress, it will return an empty string
 * @throws Error if the request was not successful, the error message will be the message from the server
 */
export async function createBackup(appId: string, backupName: string, onlyAppData: boolean): Promise<string> {
    let params = new URLSearchParams();
    params.append("package", appId);
    params.append("backupname", backupName);
    params.append("onlyappdata", onlyAppData.toString());

    let result = await fetch(`/api/backup?${params}`, { method: "GET" });

    if (result.ok) {
        return "";
    }
    if (result.status == 400) {
        let text = await result.text();
        throw new Error(text);
    }
    if (result.status == 202) {
        return await result.text();
    }
    throw new Error("Unknown error");
}

/**
 * Gets the total size of all backups as a human readable string (e.g. 1.2 GB)
 */
export async function getTotalBackupsSize() {
    let result = await fetch(`/api/allbackups`);
    return await result.text();
}

/**
 * Restore game data from a backup
 * @param appId  app id (com.beatgames.beatsaber)
 * @param backupName  backup name
 * @returns  true if the game data was restored
 */
export async function restoreGameData(appId: string, backupName: string) {
    let result = await fetch(`/api/restoregamedata?package=${appId}&backupname=${backupName}`);
    if (result.ok) {
        return true;
    }
    if (result.status == 400) {
        let text = await result.text();
        throw new Error(text);
    }
    if (result.status == 500) {
        let text = await result.text();
        throw new Error(text);
    }
}

export enum BackupStatus {
    InProgress,
    Success,
    Error
}
interface IBackupStatusResponse {
    status: BackupStatus;
    message: string;
}

/**
 * Gets the current backup status
 * @returns
 * - InProgress if the backup is in progress
 * - Success if the backup was successful
 * - Error if the backup failed 
 * - message will contain the message from the server
 * @throws Error if the request was not successful
 * @example
 * ```ts
 * let status = await getBackupCurrentStatus();
 * if (status.status == BackupStatus.InProgress) {
 *    // backup is in progress
 * }
 * ```
 * 
 **/
export async function getBackupCurrentStatus(): Promise<IBackupStatusResponse> {
    let result = await fetch(`/api/backupstatus`);

    if (result.status == 200) {
        return {
            status: BackupStatus.Success,
            message: await result.text()
        };
    }
    if (result.status == 202) {
        return {
            status: BackupStatus.InProgress,
            message: await result.text()
        };
    }
    return {
        status: BackupStatus.Error,
        message: await result.text()
    };
}

