interface getCosmeticsTypesResponse {
    /**
     * The file types that are supported by the server
     */
    fileTypes: {
        [key: string]: {
            /**
             * The name of the file type (e.g. Sabers)
             */
            name: string;
            /**
             * The file extension of the file (e.g. .qsaber)
             */
            fileType: string;
            /**
             * The directory where the files are stored relative to the root of the sdcard
             */
            directory: string;
            /**
             * Whether or not the file type requires a modded game
             */
            requiresModded: boolean;
        }
    }
}

export async function getCosmeticsTypes(): Promise<getCosmeticsTypesResponse> {
    let result = await fetch("/api/cosmetics/types");
    return await result.json();
}


export async function getInstalledCosmetics(type: string): Promise<any> {
    let result = await fetch(`/api/cosmetics/getinstalled?type=${type}`);
    return await result.json();
}

/**
 * Deletes a cosmetic
 * @param type type of cosmetic (e.g. sabers)
 * @param name name of the cosmetic (e.g. MySaber.qsaber)
 * @returns 
 */
export async function DeleteCosmetic(type: string, name: string) {
    let result = await fetch(`/api/cosmetics/delete?type=${type}&filename=${name}`, {method: "DELETE"});
    return await result.json();
}

