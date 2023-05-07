import { Link } from "@solidjs/router";
import RunButton from "../components/Buttons/RunButton";
import { CustomModal } from "./CustomModal";
import { For, createMemo, splitProps } from "solid-js";
import { config } from "../store";
import { Box, Typography } from "@suid/material";

let gameLinks: {
    [key: string]: {
        name: string,
        url: string
    }[]
} = {
    "com.beatgames.beatsaber": [
        {
            name: "Integrated Mod Manager",
            url: "/bsmods"
        },
        {
            name: "BeatSaver",
            url: "https://beatsaver.com"
        }
    ],
}



export default function GetMoreMods(props: { open: boolean, onClose: () => void }) {
    let [local, other] = splitProps(props, ['open', 'onClose']);

    let buttons = createMemo(() => {
        if (config()?.currentApp == null) return undefined;
        if (!(config()!.currentApp in gameLinks)) return undefined;
        // @ts-ignore
        return gameLinks[config()!.currentApp];
    });


    return (
        <CustomModal
            title="Get more mods"
            onClose={local.onClose}
            open={local.open}
            offsetLess={false}
        >
            <Typography fontSize={"10px"}>
                Here you can get more mods from one of these links
            </Typography>
            <Box sx={{
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
                gap: "1rem",
                mt: "0.5rem",
                alignContent: "str",
                flexWrap: "wrap"
            }}>


                <For each={buttons() ?? []}>
                    {(link) => (
                        <Box sx={{

                        }}>
                            <Link href={link.url} onClick={local.onClose}>
                                <RunButton text={link.name}>mods</RunButton>
                            </Link>

                        </Box>
                    )}
                </For>
            </Box>
        </CustomModal>
    )
}