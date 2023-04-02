interface getBackupsResponse {
    backups: Array<{
        backupName: string;
        backupLocation: string;
        containsGamedata: boolean;
        backupSize: number;
        backupSizeString: string;
    }>;
    lastRestored: string;
    backupsSize: number;
    backupsSizeString: string;
}

/**
 * Gets the backups for the given app
 * @param appId com.beatgames.beatsaber
 */
export async function getBackups(appId:string): Promise<getBackupsResponse> {
    let result = await fetch(`/api/backups?package=${appId}`);
    return await result.json();
}

export async function restoreBackup(appId:string, backupName:string) {
    let result = await fetch(`/api/backups/restore?package=${appId}&backup=${backupName}`);
    return await result.json();
}

export async function deleteBackup(appId:string, backupName:string) {
    let params = new URLSearchParams();
    params.append("package", appId);
    params.append("backupname", backupName);

    let result = await fetch(`/api/backup?${params}`, {method: "DELETE"});
    return await result.json();
}