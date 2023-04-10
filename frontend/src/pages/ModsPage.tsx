import { For, Index, JSX, Show, batch, createEffect, createMemo, createSignal, mapArray, onCleanup, onMount } from "solid-js";
import { DeleteMod, IMod, InstallModFromUrl, UpdateModState, UploadMod, getModsList } from "../api/mods";
import image from "./../assets/DefaultCover.png"
import "./ModsPage.scss";
import { modsList, mutateMods, refetchMods } from "../state/mods";
import { CompareStringsAlphabetically, Sleep } from "../util";
import toast from "solid-toast";
import { Title } from "@solidjs/meta";
import PageLayout from "../Layouts/PageLayout";
import Box from "@suid/material/Box";
import RunButton from "../components/Buttons/RunButton";
import { PlusIcon, UploadRounded } from "../assets/Icons";
import PlayArrowRounded from '@suid/icons-material/PlayArrowRounded';
import { IconButton, List, ListItem, Switch, Typography } from "@suid/material";
import CloseRounded from "@suid/icons-material/CloseRounded";
async function UploadModClick() {
  var input = document.createElement('input');
  input.type = 'file';
  input.multiple = true;
  input.click();
  input.addEventListener("change", async function (e) {

    if (this.files && this.files.length > 0) {
      for (const file of this.files) {
        await UploadMod(file);
      }
    }
    console.log("done")
    refetchMods();
    toast.success("Mods installed");
  })
}


async function onFileDrop(e: DragEvent) {
  e.preventDefault();
  e.stopPropagation();

  // If dropped items aren't files, reject them
  if (!e.dataTransfer) return;

  // If it's files, process them and send them to the server one by one
  if (e.dataTransfer) {
    let filesToUpload: Array<File> = [];


    // Try 2 ways of getting files 
    if (e.dataTransfer.items) {
      // Use DataTransferItemList interface to access the file(s)
      [...e.dataTransfer.items].forEach((item, i) => {
        // If dropped items aren't files, reject them
        if (item.kind === 'file') {
          const file = item.getAsFile();
          if (file) {
            console.log(`… file[${i}].name = ${file.name}`);
            filesToUpload.push(file);
          }
        }
      });
    } else {
      // Use DataTransfer interface to access the file(s)
      [...e.dataTransfer.files].forEach((file, i) => {
        filesToUpload.push(file);
      });
    }

    let url = e.dataTransfer.getData("URL");
    if (url) {
      await InstallModFromUrl(url);
      await Sleep(2000);
    }

    if (filesToUpload.length > 0) {
      for (const file of filesToUpload)
        await UploadMod(file);

      refetchMods();
      toast.success("Mods installed");
    }

  }

}

export default function ModsPage() {
  const [isDragging, setIsDragging] = createSignal(false);
  const [dragCounter, setDragCounter] = createSignal(0);

  function ondragenter(e: DragEvent) {

    e.preventDefault();
    e.stopPropagation();
    if (dragCounter() + 1 >= 0) {
      setIsDragging(true);
    }
    setDragCounter(dragCounter() + 1);
  }
  function ondragleave(e: DragEvent) {
    e.preventDefault();
    e.stopPropagation();
    setDragCounter(dragCounter() - 1);
    if (dragCounter() <= 0) {
      setIsDragging(false);
    }
  }

  function ondragover(e: DragEvent) {
    e.preventDefault();
    e.stopPropagation();
  }

  function ondrop(e: DragEvent) {
    onFileDrop(e);
    setIsDragging(false);
  }

  onMount(async () => {
    window.addEventListener("drop", ondrop);
    window.addEventListener("dragover", ondragover);
    window.addEventListener("dragleave", ondragleave);
    window.addEventListener("dragenter", ondragenter);
    console.log("mounted")
  })

  onCleanup(() => {
    window.removeEventListener("drop", ondrop);
    window.removeEventListener("dragover", ondragover);
    window.removeEventListener("dragleave", ondragleave);
    window.removeEventListener("dragenter", ondragenter);
    console.log("unmounted")
  })

  let filteredData = createMemo<{ mods?: Array<IMod>, libs?: Array<IMod> }>(() => ({
    mods: modsList()?.filter((s) => !s.IsLibrary).sort((a, b) => CompareStringsAlphabetically(a.Name, b.Name)),
    libs: modsList()?.filter((s) => s.IsLibrary).sort((a, b) => CompareStringsAlphabetically(a.Name, b.Name))
  }));

  return (
    <PageLayout>
      <div
        class=" contentItem modsPage"
      >
        <Box sx={{
          display: "flex",
          width: "100%",
          gap: 1,
          flexWrap: "wrap",
          justifyContent: "space-between",
          marginBottom: 2,
        }}>
          <Box sx={{
            display: "flex",
            gap: 2,
            alignItems: "center",
          }}>
            <RunButton text='Run the app' variant="success" icon={<PlayArrowRounded />} onClick={refetchMods} />
            <RunButton text='Upload a mod' icon={<UploadRounded />} onClick={UploadModClick} />
            <span style={{
              "font-family": "Roboto",
              "font-style": "normal",
              "font-weight": "400",
              "font-size": "12px",
              "line-height": "14px",
              "display": "flex",
              "align-items": "center",
              "text-align": "center",
              "color": "#D1D5DB",
              "margin-left": "10px",
            }} class="text-accent" >
              Get more mods
            </span>
          </Box>

          <Box sx={{
            display: "flex",
            gap: 2,
            alignItems: "center",
          }}>
            <RunButton text='Delete all' onClick={() => { }} style={"width: 80px"} />
          </Box>
        </Box>

        <div classList={{
          "dragOverlay": true,
          "active": isDragging()
        }
        }>
          <div class="dragOverlayText">Drop to install</div>
        </div>
        <List sx={{
          display: "flex",
          flexDirection: "column",
          gap: 1,
        }}>
          <For each={filteredData().mods} fallback={<div>Emptiness..</div>}  >
            {(mod) => (
              <ModCard mod={mod} />
            )}
          </For>
        </List>
        <h2>Installed Libraries</h2>
        <List sx={{
          display: "flex",
          flexDirection: "column",
          gap: 1,
        }}>
          <Index each={filteredData()?.libs} fallback={<div class="emptyText">No mods</div>}>
            {(mod) => (
              <ModCard mod={mod()} />
            )}
          </Index>
        </List>
      </div>
    </PageLayout>
  )
}

async function ToggleModState(modId: string, newState: boolean) {
  await UpdateModState(modId, newState);
  await Sleep(300);
  refetchMods();
  toast.success(`Mod ${modId} is ${newState ? "enabled" : "disabled"}`)
}

async function DeleteModClick(mod: IMod) {
  await DeleteMod(mod.Id);
  await Sleep(300);
  refetchMods();
  toast.success(`Mod ${mod.Name} is deleted`)
}

function ModCard({ mod }: { mod: IMod }) {
  return (
    <ListItem class="mod" sx={{
      display: "flex",
      width: "100%",
      backgroundColor: "#111827",
      borderRadius: "6px"
    }}>
      <img class="modCover" style={{
        "margin-right": "14px",
        "border-radius": "6px",
        "object-fit": "cover",

        "width": "160px",
        "height": "92px",
        "aspect-ratio": "16 / 9",
      }} src={(mod.hasCover) ? `/api/mods/cover?id=${mod.Id}` : image} />
      <Box
        sx={{
          flexGrow: 1,
        }}
      >
        <Box sx={{
          display: "flex",
          alignItems: "center",
          flexWrap: "wrap",
        }}>
          <Typography variant="h6" sx={{
            fontFamily: 'Roboto',
            fontStyle: 'normal',
            fontWeight: 400,
            fontSize: '16px',
            lineHeight: '19px',
            color: '#FFFFFF',
            marginRight: 1,


          }}  >{mod.Name}</Typography>
          <Typography variant="caption" sx={{

            fontFamily: 'Roboto',
            fontStyle: 'normal',
            fontWeight: 400,
            fontSize: '10px',
            lineHeight: '12px',
          }} class="text-accent"  >v{mod.VersionString} {mod.Author ? `by ${mod.Author}` : ""}</Typography>
        </Box>


        <Typography sx={{
          fontFamily: 'Roboto',
          fontStyle: 'normal',
          fontWeight: 400,
          fontSize: '10px',
          lineHeight: '12px',
          color: '#D1D5DB',
          marginTop: 1,
        }}>{mod.Description}</Typography>
      </Box>
      <Box sx={{
        display: "flex",
        flexDirection: "column",
        justifyContent: "space-between",
      }}>
        <IconButton onClick={() => DeleteModClick(mod)}>
          <CloseRounded />
        </IconButton>
        <Switch checked={mod.IsInstalled} onChange={() => ToggleModState(mod.Id, !mod.IsInstalled)} />
      </Box>
    </ListItem>
  )
}
