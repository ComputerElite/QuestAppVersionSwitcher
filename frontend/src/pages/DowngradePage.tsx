import { createSignal } from "solid-js";
import { IsOnQuest } from "../util";
import './DowngradePage.scss'

export default function DowngradePage() {
  const [iframeSrc, setIframeSrc] = createSignal(`https://oculusdb.rui2015.me/search?query=Beat+Saber&headsets=MONTEREY%2CHOLLYWOOD${IsOnQuest() ? `&isqavs=true` : ``}`);

  return (
    <div class="contentItem downgradePage">
      {/* <b style="font-size: 4em;" id="downgradeLoginMsg">To downgrade you must first log in in the tools & options tab!</b> */}
      <iframe id="downgradeframe" src={iframeSrc()} onLoad={(e) =>{ console.log(e.currentTarget.src) }}></iframe>
    </div>
  )
}
