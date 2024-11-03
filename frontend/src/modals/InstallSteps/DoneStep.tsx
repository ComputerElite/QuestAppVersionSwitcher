import { onMount, Show } from "solid-js";
import { refetchAppInfo, refetchModdingStatus, refetchSettings } from "../../store";
import { refetchMods } from "../../state/mods";
import { useNavigate } from "@solidjs/router";
import { GlobalPatchingData } from "../PatchingModal";
import { launchCurrentApp } from "../../api/android";
import { CustomModal } from "../CustomModal";
import RunButton from "../../components/Buttons/RunButton";
import PlayArrowRounded from "@suid/icons-material/PlayArrowRounded";
import { showConfirmModal } from "../ConfirmModal";
import { SmallText } from "../../styles/TextStyles";

/**
 * This step is shown when the patching/installing/restoring is done
 * @param props 
 * @returns 
 */
export function DoneStep(props: {
    next: () => void,
    onClose?: () => void,
    patchingData: GlobalPatchingData
    customMessage?: string
}) {

    let navigate = useNavigate();
    function startGame() {
        launchCurrentApp();
    }

    // Refresh everything just to be sure
    onMount(() => {
        refetchModdingStatus();
        refetchAppInfo();
        refetchMods();
        refetchSettings();
    })

    return (
        <CustomModal title={"Patching is done!"} open onClose={props.onClose}
            buttons={<>
                {/* If success, close the modal */}
                <RunButton text="Start game" icon={<PlayArrowRounded />} onClick={async () => {
                    let sure = await showConfirmModal({
                        title: "Start game",
                        message: "Are you sure you want to start the game without installing mods? Patched game with no mods does not make sense if you are not a developer.",
                    })

                    if (sure) {
                        props?.onClose && props.onClose(); startGame();
                    } else {
                        return;
                    }
                }} />

                <RunButton text="Install mods" variant='success' onClick={() => {
                    props?.onClose && props.onClose();
                    navigate("/mods");
                }} />
            </>} >

            <Show when={props.customMessage}>
                <SmallText>{props.customMessage}</SmallText>
            </Show>
            <Show when={!props.customMessage}>
                <SmallText>
                    The game is successfully installed, you can now start the game or install the mods.
                </SmallText>
            </Show>
        </CustomModal>
    )
}
