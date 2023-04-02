import { For, Show, createEffect, createSignal } from "solid-js";
import { ILibrary, IMod, getModsList } from "../api/mods";
import image from "./../assets/DefaultCover.png"

export default function ModsPage() {
  var loading = false;

  let [mods, setMods] = createSignal<Array<IMod>>([]);
  let [libs, setLibs] = createSignal<Array<ILibrary>>([]);

  createEffect(() => {
    console.log("mods", mods());
    console.log("libs", libs());
  });

  createEffect(async () => {
    try {
      let data = await getModsList();
      setMods(data.mods);
      setLibs(data.libs);

      console.log("success")
    } catch (e) {
      console.error(e);
    }

  });

  return (
    <div>
      <b>Please note that mod installation is in early access and there may be issues. If you experience issues please report them on the OculusDB Discord Server at <code>discord.gg/zwRfHQN2UY</code></b>
      <div class="button topMargin" onClick="LaunchApp()">Launch Game</div>
      <div class="button topButtonMargin" id="installModButton" onclick="UploadMod()">Install a Mod from Disk</div>
      <div id="operations">
        <h2 id="ongoingCount">Ongoing operations:</h2>
        <Show when={loading}>
          <div class="loaderContainer" style="margin-bottom: 10px;">
            <div class="loaderBarRight"></div>
            <div class="loaderBarLeft"></div>
            <div class="loaderBarTop"></div>
            <div class="loaderBarBottom"></div>
            <div class="loaderSpinningCircle"></div>
            <div class="loaderMiddleCircle"></div>
            <div class="loaderCircleHole"></div>
            <div class="loaderSquare"></div>
          </div>
        </Show>

        <div class="infiniteList" id="operationsList">

        </div>
      </div>
      <h2>Installed Mods</h2>
      <div class="infiniteList" id="modsList">
        <For each={mods()}>
          {(mod) => (
            <ModCard mod={mod} />
          )}

        </For>
      </div>
      <h2>Installed Libraries</h2>
      <div class="infiniteList" id="libsList">
        <For each={libs()}>
          {(mod) => (
            <ModCard mod={mod} />
          )}
        </For>
      </div>
    </div>
  )
}


function ModCard({ mod }: { mod: IMod }) {
  return (
    <div class="mod">
      <div class="leftRightSplit">
        <img class="modCover" src={image} />
        <div class="upDownSplit spaceBetween">
          <div class="upDownSplit">
            <div class="leftRightSplit nomargin">
              <div>Nya</div>
              <div class="smallText version">{mod.VersionString}</div>
            </div>
            <div class="smallText">{mod.Description}</div>
          </div>

          <div class="button" onclick="DeleteMod('Nya')">Delete</div>
        </div>
      </div>
      <div class="upDownSplit spaceBetween relative">
        <div class="smallText margin20">
          (by FrozenAlex)
        </div>

        <label class="switch">
          <input onchange="UpdateModState('Nya', false)" type="checkbox" checked="" />
          <span class="slider round"></span>
        </label>

      </div>
    </div>
  )
}