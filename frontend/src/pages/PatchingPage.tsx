import { Title } from "@solidjs/meta";
import { showChangeGameModal } from "../modals/ChangeGameModal";
import Checkbox from "@suid/material/Checkbox"
import PageLayout from "../Layouts/PageLayout";
import RunButton from "../components/Buttons/RunButton";
import PlayArrowRounded from "@suid/icons-material/PlayArrowRounded";
import { FirePatch } from "../assets/Icons";
import { appInfo, config, moddingStatus, refetchModdingStatus } from "../store";
import { Show, createEffect, createSignal } from "solid-js";
import { CustomModal } from "../modals/CustomModal";
import { FiRefreshCcw } from "solid-icons/fi";
import { Typography } from "@suid/material";

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

export default function PatchingPage() {
  let [isPatchingModalOpen, setIsPatchingModalOpen] = createSignal(false);
  createEffect(() => {
    console.log(appInfo()?.version)
  })

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
          <RunButton text={`${moddingStatus()?.isPatched ? 'Repatch' : 'Patch'} the current game`} icon={<FirePatch />} variant='success' onClick={() => { setIsPatchingModalOpen(true) }} />
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
        {/* <div id="patchingOptions">
          <h2>Patching options</h2>
          <label class="option">
            <label class="switch normal">
              <input id="externalstorage" type="checkbox" checked />
              <span class="slider round"></span>
            </label>
            Add external storage permission
          </label>
          <br />

          <label class="option">
            <label class="switch normal">
              <input id="handtracking" type="checkbox" checked />
              <span class="slider round"></span>
            </label>
            Add hand tracking permission
          </label>
          <br />
          <label class="option">
            Hand tracking version
            <select id="handtrackingversion">
              <option value="3">V2</option>
              <option value="2">V1 high frequency</option>
              <option value="1">V1</option>
            </select>
          </label>
          <br />
          <br />
          <label class="option">
            <label class="switch normal">
              <input id="debug" type="checkbox" checked />
              <span class="slider round"></span>
            </label>
            Add Debug option
          </label>
          <br />
          <br />
          <input type="text" id="otherName" placeholder="permission name" />
          <br />
          <br />
          <div class="button" onclick="AddPermission()">Add Permission</div>
          <br />
          <div id="other">
          </div>
        </div> */}
      </div>
    </PageLayout>

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