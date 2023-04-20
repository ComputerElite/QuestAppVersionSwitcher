import { Title } from "@solidjs/meta";
import PageLayout from "../Layouts/PageLayout";
import { For, Show } from "solid-js";
import { Box } from "@suid/material";
import { A } from "@solidjs/router";
import RunButton from "../components/Buttons/RunButton";
import FastRewindSharp from "@suid/icons-material/FastRewindSharp"

let progressBars: [] = [

]


export default function DownloadProgressPage() {
  return (
    <PageLayout>
      <div class="contentItem">
        <Title>Downloads</Title>
        <div id="progressBarContainers" style="width: 95%;">
          <Show when={progressBars.length === 0}>
            <Box sx={{
              display: "flex",
              justifyContent: "center",
              flexDirection: "column",
              alignItems: "center",
              textAlign: "center",
            }}>
              <Box sx={{
                marginBottom: "1rem",
              }}>
                No downloads yet, to download a game go to
              </Box>
              <Box>
                <A href="/downgrade/">
                  <RunButton  text="Downgrade" icon={<FastRewindSharp />}></RunButton>
                </A>
              </Box>
            </Box>
          </Show>
          <Show when={progressBars.length > 0}>
            <For each={progressBars}>
              {(progressBar) => (
                <div class="progressBarContainer">
                  <div class="progressBar">sdas
                  </div>
                </div>)}
            </For>
          </Show>

        </div>
      </div>
    </PageLayout>

  )
}
