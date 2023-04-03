import toast from "solid-toast";

type getDownloadsResponse = Array<{
    percentage: number;
    percentageString: string;
    done: number;
    total: number;
    speed: number;
    eTASeconds: number;
    doneString: string;
    totalString: string;
    speedString: string;
    eTAString: string;
    name: string;
    backupName: string;
    textColor: string;
}>

export async function getDownloads(): Promise<getDownloadsResponse> {
    let result = await fetch("/api/downloads");
    return await result.json();
}


interface downloadRequest {
    binaryId: string;
    password: string;
    version: string;
    app: string;
    parentId: string;
    isObb: boolean;
    packageName: string;
}

export async function downloadOculusGame(options: downloadRequest) {
    let result = await fetch(`/api/download`, {
        method: "POST",
        body: JSON.stringify(options)
    });

    if (result.status == 403) {
        let text = await result.text();
        throw new Error(text)
    }

    return await result.json();
}

/**
 * Cancel a download from oculus
 * @param backupName the name from the download progress object
 */
export async function cancelOculusDownload(backupName: string) {
    await fetch(`/api/canceldownload?name=${backupName}`);
    return;
}
