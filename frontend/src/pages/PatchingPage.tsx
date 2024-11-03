import { Title } from "@solidjs/meta";
import { showChangeGameModal } from "../modals/ChangeGameModal";
import Checkbox from "@suid/material/Checkbox"
import PageLayout from "../Layouts/PageLayout";
import RunButton from "../components/Buttons/RunButton";

import { FirePatch } from "../assets/Icons";
import { appInfo, config, moddingStatus, patchingOptions, refetchModdingStatus, refetchPatchingOptions } from "../store";
import { For, Show, createEffect, createSignal, onCleanup } from "solid-js";
import { FiRefreshCcw } from "solid-icons/fi";
import { Box, MenuItem, Select, Switch, TextField, Typography, Chip } from "@suid/material";
import { OptionHeader } from "./ToolsPage";
import { ConvertImageToPNG, DownloadImageAsBlob, DownloadImageAsPNG, FileToBase64, GetGameName } from "../util";
import { HandtrackingType, IPatchOptions, ModLoaderType, getPatchedModdingStatus, patchCurrentApp, setPatchingOptions } from "../api/patching";
import toast from "solid-toast";
import { isPackageInstalled } from "../api/android";
import { refetchBackups } from "../state/backups";
import PatchingModal from "../modals/PatchingModal";
import { FileDropper } from "../components/FileDropper";
import { proxyUrl } from "../api/app";

export default function PatchingPage() {
  let [isPatchingModalOpen, setIsPatchingModalOpen] = createSignal(false);
  createEffect(() => {
    console.log(appInfo()?.version)
  })

  async function startPatching() {
    // Should never happen, but just in case
    if (!config()?.currentApp) return toast.error("No game selected");
    if (!await isPackageInstalled(config()!.currentApp)) {
      // Something desynced, refetch the modding status
      refetchModdingStatus();
      return toast.error("Game is not installed");
    }

    setIsPatchingModalOpen(true);
  }



  function onPatchFinished() {
    setIsPatchingModalOpen(false);
    refetchModdingStatus();
    refetchBackups();
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
              <Switch checked={patchingOptions()?.externalStorage} onChange={
                () => {
                  updatePatchingOptions({ externalStorage: !patchingOptions()?.externalStorage })
                }
              } />
              <OptionText>Add external storage permission</OptionText>
            </Box>
            <Box sx={{ display: "flex", gap: 2, alignItems: "center", }}>
              <Switch checked={patchingOptions()?.debug ?? false} onChange={() => { updatePatchingOptions({ debug: !patchingOptions()?.debug }) }
              } />
              <OptionText>Add debug option</OptionText>
            </Box>
            <Box sx={{ display: "flex", gap: 2, alignItems: "center", }}>
              <Switch checked={patchingOptions()?.otherPermissions?.includes("android.permission.RECORD_AUDIO") ?? false}

                onChange={() => {
                  toggleAdditionalPermission("android.permission.RECORD_AUDIO")
                }} />
              <OptionText>Microphone permission</OptionText>
            </Box>
            <Box sx={{ display: "flex", gap: 2, alignItems: "center", }}>
              <Switch checked={patchingOptions()?.openXR ?? false} onChange={() => { updatePatchingOptions({ openXR: !patchingOptions()?.openXR }) }
              } />
              <OptionText>OpenXR (Mixed Reality)</OptionText>
            </Box>
            <Box sx={{ display: "flex", gap: 2, alignItems: "center", }}>
              <Switch checked={patchingOptions()?.handTracking ?? false} onChange={() => { updatePatchingOptions({ handTracking: !patchingOptions()?.handTracking }) }
              } />
              <OptionText>Hand tracking</OptionText>
            </Box>
          </Box>

          {/* Hand tracking */}
          <Show when={config()?.currentApp && patchingOptions()?.handTracking}>
            <Box sx={{ marginY: 3 }}>
              <OptionHeader>Hand tracking version</OptionHeader>
              <Box sx={{ display: "flex", gap: 2, alignItems: "center", }}>
                <Select sx={{ minWidth: 200, }} size="small" variant="outlined" color="primary"
                  value={patchingOptions()?.handTrackingVersion ?? null} onChange={(e) => { updatePatchingOptions({ handTrackingVersion: e.target.value }) }}>
                  <MenuItem value={HandtrackingType.None}>Default (Recommended)</MenuItem>
                  <MenuItem value={HandtrackingType.V2}>V2</MenuItem>
                  <MenuItem value={HandtrackingType.V2_1}>V2.1</MenuItem>
                </Select>
              </Box>
            </Box>
          </Show>

          {/* Hand tracking */}
          <Show when={config()?.currentApp && patchingOptions()?.handTracking}>
            <Box sx={{ marginY: 3 }}>
              <OptionHeader>Modloader</OptionHeader>
              <Box sx={{ display: "flex", gap: 2, alignItems: "center", }}>
                <Select sx={{ minWidth: 200, }} size="small" variant="outlined" color="primary"
                  value={patchingOptions()?.modloader ?? null} onChange={(e) => { updatePatchingOptions({ modloader: e.target.value }) }}>
                  <MenuItem value={ModLoaderType.QuestLoader}>QuestLoader (Quest 2)</MenuItem>
                  <MenuItem value={ModLoaderType.Scotland2}>Scotland2 (Quest 3)</MenuItem>
                </Select>
              </Box>
            </Box>
          </Show>

          {/* Additional permissions */}
          <Box sx={{ marginY: 3 }}>
            <OptionHeader>Additional permissions</OptionHeader>
            <form onSubmit={(e) => {
              e.preventDefault();
              let formData = new FormData(e.target as HTMLFormElement);
              let permission = formData.get("permission") as string;
              if (!permission) return;
              toggleAdditionalPermission(permission, true);
            }}>
              <Box class="flex gap-0 items-center mt-1">
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
                  }} text='Add permission' variant="success" type="submit" />
              </Box>

              <Box class="flex gap-1 items-center flex-wrap mt-1">
                <For each={patchingOptions()?.otherPermissions}>
                  {(permission, index) => {
                    return (
                      <Chip label={permission} sx={{ marginTop: 1 }} onDelete={() => {
                        toggleAdditionalPermission(permission, false);
                      }} />
                    )
                  }}
                </For>
              </Box>
            </form>
          </Box>

          {/* TODO: Design a features control */}
          {/* Additional features
          <Box sx={{ marginY: 3 }}>
            <OptionHeader>Additional features</OptionHeader>
            <form onSubmit={(e) => {
                e.preventDefault();
                let formData = new FormData(e.target as HTMLFormElement);
                let feature = formData.get("feature") as string;
                if (!feature) return;
                toggleAdditionalFeature(feature, true);
            }}>
              <Box class="flex gap-0 items-center mt-1">
                <TextField name="feature" placeholder="" sx={{
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
                  }}
                  text='Add feature'  variant="success" type="submit" />
              </Box>
              <Box class="flex gap-1 items-center flex-wrap mt-1">
                <For each={patchingOptions()?.otherFeatures}>
                  {(feature) => {
                    return (
                      <Chip label={feature.name} sx={{ marginTop: 1 }} onDelete={() => {
                        toggleAdditionalFeature(feature.name, false);
                      }} />
                    )
                  }}
                </For>
              </Box>
            </form>
          </Box> */}
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
        <Show when={isPatchingModalOpen()} >
          <PatchingModal
            open={isPatchingModalOpen()} onClose={() => { setIsPatchingModalOpen(false) }} onPatchFinished={onPatchFinished}
          />
        </Show>

        <CustomSplashScreenPicker />

        <Box sx={{ marginY: 3 }}>
          <OptionHeader>Advanced</OptionHeader>
          <Box sx={{ display: "flex", gap: 2, alignItems: "center", }}>
            <Switch checked={patchingOptions()?.resignOnly ?? false}
              onChange={() => { updatePatchingOptions({ resignOnly: !patchingOptions()?.resignOnly }) }
              } />
            <OptionText>Resign only</OptionText>
          </Box>
        </Box>
      </div>
    </PageLayout >

  )
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

function CustomSplashScreenPicker() {
  return (
    <Box sx={{ marginY: 3 }}>
      <OptionHeader>Custom splash screen</OptionHeader>
      <FileDropper class="flex gap-2 items-center"
        overlayText={"Drag and drop an image, if you want transparent background use a png file"}
        onFilesDropped={async (files) => {
          let file = files[0];
          if (!file) return;
          updateSplashImage(file)

        }}
        onUrlDropped={async (url) => {
          console.log(url)
          const imageFile = await DownloadImageAsPNG(proxyUrl(url));
          updateSplashImage(imageFile);
        }}
      >
        <Show when={patchingOptions()?.splashImageBase64}>
          <img src={patchingOptions()?.splashImageBase64} style={{ width: "100px", "max-height": "100px", "object-fit": "cover" }} />

          <RunButton text="Remove" onClick={() => {
            updatePatchingOptions({ splashImageBase64: "" });
          }} />
        </Show>
        <RunButton text="Upload" onClick={() => {
          let input = document.createElement("input");
          input.type = "file";
          input.accept = "image/*";
          input.onchange = (e) => {
            let file = input.files?.[0];
            if (!file) return;
            updateSplashImage(file)
          }
          input.click();
        }} />
      </FileDropper>


    </Box>
  )
}

// State utils
export async function updatePatchingOptions(options: Partial<IPatchOptions>) {
  let newOptions = patchingOptions();
  if (!newOptions) return console.warn("Patching options are null");

  newOptions = { ...newOptions, ...options }

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
   * Toggles an additional permission 
   * @param permission The permission to toggle
   * @param enable Whether to enable or disable the permission (optional)
   */
export function toggleAdditionalPermission(permission: string, enable?: boolean) {
  let options = patchingOptions();
  if (!options) return;

  enable = enable ?? !options.otherPermissions?.includes(permission);

  const filteredPermissions = options.otherPermissions.filter((i) => i !== permission);

  if (enable) {
    updatePatchingOptions({ otherPermissions: [...filteredPermissions, permission] })
  } else {
    updatePatchingOptions({ otherPermissions: filteredPermissions })
  }
}


async function updateSplashImage(file: File) {
  // Check if the file is a png
  if (file.type != "image/png") {
    file = await ConvertImageToPNG(file);
    if (!file) {
      toast.error("Failed to convert image to png");
      return;
    }
  }

  let base64 = await FileToBase64(file);
  if (!base64) {
    toast.error("Failed to convert image to base64");
    return;
  };

  updatePatchingOptions({ splashImageBase64: base64 });
}