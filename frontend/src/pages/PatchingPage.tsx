import { Title } from "@solidjs/meta";
import { showChangeGameModal } from "../modals/ChangeGameModal";

export default function PatchingPage() {
  return (
    <div class="contentItem">
      <Title>Patching</Title>
      <div class="button"  onClick={showChangeGameModal}>Change app</div>
      <h3>Currently selected game: <div class="inline packageName">some game</div></h3>
      <div id="patchStatus">Loading...</div>
      <div class="textbox" id="patchingTextBox"></div>

      <div id="patchingOptions">
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
      </div>
    </div>
  )
}
