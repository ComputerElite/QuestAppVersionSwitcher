import { createSignal } from "solid-js";
import { IsOnQuest } from "../util";
import './DowngradePage.scss'
import { Title } from "@solidjs/meta";
import PageLayout from "../Layouts/PageLayout";

export default function DowngradePage() {
  const [iframeSrc, setIframeSrc] = createSignal(`https://oculusdb.rui2015.me/search?query=Beat+Saber&headsets=MONTEREY%2CHOLLYWOOD${IsOnQuest() ? `&isqavs=true` : ``}`);

  return (
    <PageLayout hasOffset={false}>
      <div class="contentItem downgradePage">
        <Title>Downgrade</Title>
        {/* <b style="font-size: 4em;" id="downgradeLoginMsg">To downgrade you must first log in in the tools & options tab!</b> */}
        <iframe id="downgradeframe" src={iframeSrc()} onLoad={(e) => { console.log(e.currentTarget.src) }}></iframe>
      </div>
    </PageLayout>

  )
}
