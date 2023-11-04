import { proxyFetch } from "./app";
import gamedata from "./testData/gameData"
import searchData from "./testData/searchResults"

export interface IOculusDBGameVersion {
    id: string,
    alias?: string,
    lastPriorityScrape: string,
    lastScrape: string,
    binary_release_channels: {
        count: number,
        nodes: Array<{
            id: string,
            channel_name: "LIVE" | string,
            latest_supported_binary: object,
        }>,
    }
    changeLog?: string,
    created_date: number,
    downloadable: boolean,
    file_name: string,
    obbList?: Array<{
        file_name: string,
        uri: string,
        size: string,
        id: string,
        is_required: boolean,
        sizeNumerical: number,
        sizeString: string,
    }>,
    parentApplication: {
        canonicalName: string,
        displayName: string,
        hmd: number,
        id: string,
    }
    platform: string,
    version: string,
    versionCode: number,
    __OculusDBType: string,
    __lastUpdated: string,
}

// BeatSaber: 2448060205267927
export async function OculusDBGetGame(oculusId: string, test: boolean = false): Promise<{
    versions: Array<IOculusDBGameVersion>
    applications: Array<IOculusDBApplication>
}> {
    if (test) {
        return gamedata;
    }

    const response = await proxyFetch(`https://oculusdb.rui2015.me/api/v1/connected/${oculusId}?onlydownloadable=true`);
    if (response.ok) {
        const json = await response.json();
        return json;
    } else {
        throw new Error("Failed to get game");
    }
}



export interface IOculusDBApplication {
    __lastUpdated: string,
    __OculusDBType: string,
    hmd: number,
    /**
     * Name of the game, used for display
     * @example Beat Saber
     */
    appName: string,
    /**
     * Name of the game package, used for downloading
     * @example com.beatgames.beatsaber
     */
    packageName: string,
    baseline_offer: {
        end_time: number,
        id: string,
        show_timer: boolean,
        price: {
            offset_amount: string,
            currency: string,
            formatted: string
        },
        promo_benefit: null,
        strikethrough_price: {
            offset_amount: string,
            currency: string,
            formatted: string
        }
    },
    canonicalName: string,
    current_gift_offer: null,
    current_offer: {
        end_time: number,
        id: string,
        show_timer: boolean,
        price: {
            offset_amount: string,
            currency: string,
            formatted: string
        },
        promo_benefit: null,
        strikethrough_price: null
    },
    displayName: string,
    display_name: string,
    display_long_description: string,
    genre_names: string[],
    has_in_app_ads: boolean,
    id: string,
    is_approved: boolean,
    is_concept: boolean,
    is_enterprise_enabled: boolean,
    organization: {
        id: string,
        is_authorized_for_quest: boolean
    },
    platform: string,
    quality_rating_aggregate: number,
    imageLink: string,
    release_channels: {
        count: number,
        nodes: Array<{
            id: string,
            channel_name: string,
            latest_supported_binary: {
                binary_application: null,
                id: string,
                version: string,
                platform: string,
                file_name: string,
                uri: string,
                change_log: null,
                changeLog: null,
                richChangeLog: string,
                firewall_exceptions_required: boolean,
                is_2d_mode_supported: boolean,
                launch_file: string,
                launch_file_2d: null,
                launch_parameters: string,
                launch_parameters_2d: null,
                Platform: string,
                release_notes_plain_text: string,
                required_space: string,
                required_space_numerical: number,
                size: string,
                size_numerical: number,
                status: string,
                supported_hmd_platforms: string[],
                supported_hmd_platforms_enum: string[],
                __isAppBinary: string,
                __isAppBinaryWithFileAsset: string,
                version_code: number,
                versionCode: number,
                created_date: number,
                binary_release_channels: null,
                lastIapItems: {
                    count: number,
                    edges: Array<any>,
                    page_info: {
                        end_cursor: string,
                        start_cursor: string
                    }
                }
                firstIapItems: {
                    count: number,
                    edges: Array<any>,
                    page_info: {
                        end_cursor: string,
                        start_cursor: string
                    }
                }
                asset_files: {
                    count: number,
                    nodes: Array<any>
                }
                obb_binary: null,
                __typename: string

            }
        }>
    },
    release_date: number,
    supported_hmd_platforms: string[],
    supported_hmd_platforms_enum: number[],
    website_url?: string,
}

export async function OculusDBSearchGame(gameId: string, test: boolean = false): Promise<IOculusDBApplication[]> {
    if (test) {
        return searchData;
    }

    let response = await proxyFetch(`https://oculusdb.rui2015.me/api/v1/search/${gameId}?headsets=MONTEREY,HOLLYWOOD,SEACLIFF`);

    if (response.ok) {
        const json = await response.json();
        return json;
    } else { 
        throw new Error("Failed to search");
        return [];
    }
}