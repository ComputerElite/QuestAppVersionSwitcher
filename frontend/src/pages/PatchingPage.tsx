import { Title } from "@solidjs/meta";
import { showChangeGameModal } from "../modals/ChangeGameModal";
import Checkbox from "@suid/material/Checkbox"
import PageLayout from "../Layouts/PageLayout";
import RunButton from "../components/Buttons/RunButton";
import PlayArrowRounded from "@suid/icons-material/PlayArrowRounded";
import { DeleteIcon, FirePatch } from "../assets/Icons";
import { InternalPatchingOptions, appInfo, config, moddingStatus, mutatePatchingOptions, patchingOptions, refetchModdingStatus, refetchPatchingOptions } from "../store";
import { For, Show, children, createEffect, createSignal, splitProps } from "solid-js";
import { CustomModal } from "../modals/CustomModal";
import { FiRefreshCcw } from "solid-icons/fi";
import { Box, MenuItem, Select, Switch, TextField, Typography, Chip } from "@suid/material";
import { OptionHeader } from "./ToolsPage";
import { createStore, produce } from "solid-js/store";
import { GetGameName } from "../util";
import { HandtrackingTypes, getPatchedModdingStatus, patchCurrentApp, setPatchingOptions } from "../api/patching";
import toast from "solid-toast";
import { isPackageInstalled } from "../api/android";


export default function PatchingPage() {
  let [isPatchingModalOpen, setIsPatchingModalOpen] = createSignal(false);
  createEffect(() => {
    console.log(appInfo()?.version)
  })

  function onAddPermission(e: SubmitEvent) {
    e.preventDefault();
    debugger
    let formData = new FormData(e.target as HTMLFormElement);
    let permission = formData.get("permission") as string;
    if (!permission) return;
    if (patchingOptions()?.additionalPermissions.includes(permission)) return;
    updatePatchingOptions({ additionalPermissions: [...patchingOptions()!.additionalPermissions, permission] })
  }

  async function startPatching() {
    // Should never happen, but just in case
    if (!config()?.currentApp) return toast.error("No game selected");
    if (!await isPackageInstalled(config()!.currentApp)) {
      // Something desynced, refetch the modding status
      refetchModdingStatus();
      return toast.error("Game is not installed");
    }
    
    try {
      let result = await patchCurrentApp();
      if (!result) {
        toast.error("Failed to patch the game");
        return;
      }
      await refetchModdingStatus();
      setIsPatchingModalOpen(true);
    } catch (e) {
      console.error(e)
      toast.error("Failed to patch game");
    }
  }

  async function updatePatchingOptions(options: Partial<InternalPatchingOptions>) {
    let newOptions = patchingOptions();
    if (!newOptions) return console.warn("Patching options are null");

    newOptions = {...newOptions, ...options} 
    
    try {
      let result = await setPatchingOptions(newOptions);
      if (!result) {
        toast.error("Failed to update patching options");
        return;
      }
      await refetchPatchingOptions();
    } catch (e) {
      console.error(e)
      toast.error("Failed to update patching options");
    }
  }

  /**
   * If the game is patched, allow user to repatch it again to change cover image and other stuff
   * If the game is not patched, allow user to patch it
   * If the game is not installed, show a message saying that the game is not installed
   * If the game is not selected, show a message saying that the game is not selected
   **/

  return (
    <PageLayout>
      <div class="contentItem">
        <Title>Patching</Title>

        <Show when={config()?.currentApp && moddingStatus()?.isInstalled}>
          <RunButton text={`${moddingStatus()?.isPatched ? 'Repatch' : 'Patch'} ${GetGameName(config()!.currentApp)}`} icon={<FirePatch />} variant='success' onClick={startPatching} />

          {/* Patching options */}
          <Box sx={{ marginY: 3 }}>
            <OptionHeader>Patching options</OptionHeader>
            <Box sx={{ display: "flex", gap: 2, alignItems: "center", }}>
              <Switch checked={patchingOptions()?.addExternalStorage} onChange={
                () => {
                  updatePatchingOptions({ addExternalStorage: !patchingOptions()?.addExternalStorage })
                }
              } />
              <OptionText>Add external storage permission</OptionText>
            </Box>
            <Box sx={{ display: "flex", gap: 2, alignItems: "center", }}>
              <Switch checked={patchingOptions()?.addDebug ?? false} onChange={
                () => {
                  updatePatchingOptions({ addDebug: !patchingOptions()?.addDebug })
                }
              } />
              <OptionText>Add debug option</OptionText>
            </Box>
          </Box>

          {/* Hand tracking */}
          <Box sx={{ marginY: 3 }}>
            <OptionHeader>Hand tracking</OptionHeader>
            <Box sx={{ display: "flex", gap: 2, alignItems: "center", }}>
              <Select
                sx={{
                  minWidth: 200,
                }}
                size="small" variant="outlined" color="primary" value={patchingOptions()?.handtracking?? null} onChange={
                  (e) => {
                    updatePatchingOptions({ handtracking: e.target.value })
                  }
                }>
                <MenuItem value={HandtrackingTypes.None}>None</MenuItem>
                <MenuItem value={HandtrackingTypes.V1}>V1</MenuItem>
                <MenuItem value={HandtrackingTypes.V1HighFrequency}>V1 high frequency</MenuItem>
                <MenuItem value={HandtrackingTypes.V2}>V2</MenuItem>
              </Select>

            </Box>

          </Box>

          {/* Additional permissions */}
          <Box sx={{ marginY: 3 }}>
            <OptionHeader>Additional permissions</OptionHeader>
            <form onSubmit={onAddPermission}>
              <Box sx={{
                display: "flex",
                gap: 0,
                alignItems: "center",
                marginTop: 1,
              }}>
                <TextField name="permission" sx={{
                  borderRadius: "222px",
                  ".MuiInputBase-root": {
                    borderRadius: "6px 0px 0px 6px",
                  }
                }} size="small" variant="outlined" color="primary"
                />
                <RunButton style={
                  {
                    height: "40px",
                    width: "100px",
                    "border-radius": "0px 6px 6px 0px",
                  }
                } text='Add permission' variant="success" type="submit" />
              </Box>

              <Box sx={{ display: "flex", gap: 1, alignItems: "center", flexWrap: "wrap", marginTop: 1 }}>
                <For each={patchingOptions()?.additionalPermissions}>
                  {(permission, index) => {
                    return (
                      <Chip label={permission} sx={{ marginTop: 1 }} onDelete={() => {
                        updatePatchingOptions({ additionalPermissions: patchingOptions()?.additionalPermissions.filter((_, i) => i !== index()) })
                      }} />
                    )
                  }}
                </For>

              </Box>
            </form>
          </Box>
        </Show>


        <Show when={config()?.currentApp && moddingStatus()?.isInstalled === false}>
          <NoticeText>
            The selected game is not installed,
            install it from the oculus store or the downgrade section.
          </NoticeText>
          <NoticeText>If you installed the game and you still see this message press the button below</NoticeText>
          <RunButton text="Refresh" icon={<FiRefreshCcw />} variant='success' onClick={() => {
            refetchModdingStatus();
          }} />
        </Show>
        <Show when={!config()?.currentApp}>
          <NoticeText>No game selected, select your game from the left menu, if it's not there, install it first</NoticeText>
        </Show>
        <PatchingModal
          open={isPatchingModalOpen()} onClose={() => { setIsPatchingModalOpen(false) }}
        />
      </div>
    </PageLayout >

  )
}


function PatchingModal(props: { open: boolean, onClose?: () => void }) {
  return <CustomModal title={"Patching modal"} open={props.open} onClose={props.onClose}
    buttons={<>
      <RunButton text="Patch" icon={<PlayArrowRounded />} variant='success' onClick={() => { }} />
    </>} >
    <div>sadaa</div>
  </CustomModal>
}


/// STYLES
const NoticeText = (props: { children: any }) => {
  return (
    <Typography sx={{
      fontFamily: 'Roboto',
      fontStyle: 'normal',
      fontWeight: 500,
      fontSize: "16px",
      lineHeight: "21px",
      marginBottom: 1,
    }} variant="h4">{props.children}</Typography>
  )
}

const OptionText = (props: { children: any }) => {
  return (
    <Typography sx={{
      fontFamily: 'Roboto',
      fontStyle: 'normal',
      fontWeight: 400,
      fontSize: "12px",
      lineHeight: "14px",
    }} variant="h4">{props.children}</Typography>
  )
}