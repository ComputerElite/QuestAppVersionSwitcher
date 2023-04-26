import { createStore } from "solid-js/store";
import { ModTask, QAVSModOperationType, getModOperations } from "../api/mods";
import { createEffect, createSignal } from "solid-js";
import toast from "solid-toast";
import { refetchModdingStatus, refetchPatchingOptions } from "../store";
import { refetchMods } from "./mods";
import { GetWSFullURL } from "../util";


enum WebSocketStatus {
    CONNECTING = 0,
    CONNECTED = 1,
    DISCONNECTED = 2,
    ERROR = 3,
}

/**
 * The task manager is used to keep track of all the tasks that are currently running.
 * 
 * We have a list of tasks, and we can add, remove, get, and get all tasks.
 */

let [tasks, setTasks] = createStore<ModTask[]>([]);
let [socketStatus, setSocketStatus] = createSignal<WebSocketStatus>(WebSocketStatus.CONNECTING);

type TaskType = "tasks-done" | "task-done" | "task-new" | "patch-progress";

class BackendEventsClass extends EventTarget {
    constructor() {
        super();
    }

    emitTasksDone() {
        const event = new CustomEvent('tasks-done');
        this.dispatchEvent(event);
    }

    emitTaskDone(task: ModTask) {
        const event = new CustomEvent('task-done', { detail: task });
        this.dispatchEvent(event);
    }

    emitNewTask(task: ModTask) {
        const event = new CustomEvent('task-new', { detail: task });
        this.dispatchEvent(event);
    }

    emitPatchingProgress(task: PatchingProgressData) {
        const event = new CustomEvent('patch-progress', { detail: task });
        this.dispatchEvent(event);
    }

    addEventListener(type: TaskType, callback: EventListenerOrEventListenerObject | null, options?: boolean | AddEventListenerOptions | undefined): void {
        super.addEventListener(type, callback, options);
    }

    removeEventListener(type: TaskType, callback: EventListenerOrEventListenerObject | null, options?: boolean | EventListenerOptions | undefined): void {
        super.removeEventListener(type, callback, options);
    }
}



export const BackendEvents = new BackendEventsClass();



let [operationsInProgress, setOperationsInProgress] = createSignal<boolean>(false);

export function addTask(task: ModTask) {
    setTasks([...tasks, task]);
}

export function removeTask(operationId: number) {
    setTasks(tasks.filter((task) => task.operationId !== operationId));
}

export function getTask(operationId: number) {
    return tasks.find((task) => task.operationId === operationId);
}

export function getTasks() {
    return tasks;
}

BackendEvents.addEventListener("task-done", async (e) => {
    let task = (e as CustomEvent).detail as ModTask;

    if (task.type == QAVSModOperationType.Error) {
        toast.error(`${task.name}`);
    } else {
        toast.success(`${task.name} `);
    }
    await refetchModdingStatus();
    await refetchMods();
});

BackendEvents.addEventListener("task-new", async (e) => {
    let task = (e as CustomEvent).detail as ModTask;
    toast(`${task.name}`);
});


BackendEvents.addEventListener("tasks-done", async (e) => {
    toast.success(`All tasks are done!`);
    await refetchModdingStatus();
    await refetchMods();
});


export interface PatchingProgressData {
    backupName: string;
    currentOperation: string;
    done: boolean;
    doneOperations: number;
    error: boolean;
    errorText: string;
    progress: number;
    progressString: string;
    totalOperations: number;
}

// Websocket connection
let ws: WebSocket | null = null;

export function InitWS() {
    ws = new WebSocket(GetWSFullURL());
    // connect to websocket one port higher than the server
    ws.onerror = function (error) {
        setSocketStatus(WebSocketStatus.ERROR);
        console.log("WebSocket Error: " + error + ". Reconnecting...");
        // reconnect
        ws = new WebSocket(GetWSFullURL());
    }

    ws.onclose = function (e) {
        setSocketStatus(WebSocketStatus.DISCONNECTED);
        console.log("WebSocket closed. Reconnecting...");
        // reconnect
        ws = new WebSocket(GetWSFullURL());
    }

    ws.onopen = function (e) {
        console.log("WebSocket connected");
        setSocketStatus(WebSocketStatus.CONNECTED);
    }

    ws.onmessage = function (e) {
        // TODO: We need to handle chunked messages here too, because the data can be split up into multiple messages if it's too big
        try {
            var data = JSON.parse(e.data);
            console.log("WS Data", data);
            if (data.route == "/api/downloads") {
                // refetch
                // TODO: Implement this
            } else if (data.route == "/api/mods/mods") {
                updateTasks(data.data.operations);
                refetchMods();
            } else if (data.route == "/api/patching/patchstatus") {
                refetchModdingStatus();
                BackendEvents.emitPatchingProgress(data.data);
            }
        }
        catch (error) {
            console.warn("Invalid WS Data", e.data);
            return;
        }

    }
}


export async function updateTasks(newState: ModTask[]) {
    try {
        let resp: ModTask[] = newState;
        
        // // Flags 
        // let hasTasksInProgress = false;

       
        // Analyze tasks that we have and tasks that we got from the backend
        resp.forEach((task) => {
            // If we have a task that is not in the list, add it
            // if (!hasTasksInProgress && !task.isDone) {
            //     hasTasksInProgress = true;
            // }

            // Compare status of old task and new task
            let oldTask = getTask(task.operationId);
            if (!oldTask) {
                // if (task.isDone) {
                //     hasTasksInProgress = true;
                // }
                if (task.isDone) {
                    BackendEvents.emitTaskDone(task);
                } else {
                    BackendEvents.emitNewTask(task);
                }

            } else {
                // We assume that the task cannot go from done to not done
                if (oldTask.isDone !== task.isDone) {
                    BackendEvents.emitTaskDone(task);
                }
            }
        });

        // Track operations in progress
        // if (!hasTasksInProgress && operationsInProgress()) {
        //     setOperationsInProgress(false);
        //     BackendEvents.emitTasksDone();
        // } else if (hasTasksInProgress && !operationsInProgress()) {
        //     setOperationsInProgress(true);
        // }



        // Set the tasks
        setTasks(resp);
    } catch (e) {
        console.error(e);
    }
}

// Fetch the mod operations on page load
createEffect(async () => {
    try {
        let operations = await getModOperations();
        setTasks(operations);
    } catch (e) {
        console.error(e);
    }   
});
