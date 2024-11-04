import {
  children,
  createSignal,
  JSX,
  onCleanup,
  onMount,
  splitProps,
} from "solid-js";
import style from "./FileDropper.module.scss";

interface FileDropperProps extends JSX.HTMLAttributes<HTMLDivElement> {
  onFilesDropped?: (e: File[]) => void;
  onUrlDropped?: (urls: string) => void;
  overlayText?: string;
  children?: any;
}

export function FileDropper(props: FileDropperProps) {
  let [local, other] = splitProps(props, [
    "children",
    "overlayText",
    "onFilesDropped",
  ]);

  const c = children(() => props.children);

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
    e.preventDefault();
    console.log("dropped", e);
    if (props.onFilesDropped) {
      const files = getFilesFromDragEvent(e);
      if (files.length > 0) {
        props.onFilesDropped(files);
      }
    }
    if (props.onUrlDropped) {
      const url = getUrlFromDragEvent(e);
      if (url) {
        props.onUrlDropped(url);
      }
    }
    setIsDragging(false);
  }

  return (
    <div
      onDragOver={ondragover}
      onDragEnter={ondragenter}
      onDragLeave={ondragleave}
      onDrop={ondrop}
      class="relative"
    >
      <div {...other}>{c()}</div>
      <div
        classList={{
          [`${style.dragOverlay}`]: true,
          [`${style.active}`]: isDragging(),
        }}
      >
        <div class={style.dragOverlayText}>
          {local.overlayText ?? "Drop file here"}
        </div>
      </div>
    </div>
  );
}

function getFilesFromDragEvent(e: DragEvent) {
  // Try 2 ways of getting files
  // If dropped items aren't files, reject them

  // If it's files, process them and send them to the server one by one
  let filesToUpload: Array<File> = [];

  // If no data transfer, return empty array
  if (!e.dataTransfer) return [];

  // Try 2 ways of getting files
  if (e.dataTransfer.items) {
    // Use DataTransferItemList interface to access the file(s)
    [...e.dataTransfer.items].forEach((item, i) => {
      // If dropped items aren't files, reject them
      if (item.kind === "file") {
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
  return filesToUpload;
}

function getUrlFromDragEvent(e: DragEvent) {
  if (!e.dataTransfer) return;

  // Get the url if there is one
  let url = e.dataTransfer.getData("URL");

  if (url) {
    return url;
  }
  return undefined;
}

async function onFileDrop(e: DragEvent) {
  e.preventDefault();
  e.stopPropagation();
}
