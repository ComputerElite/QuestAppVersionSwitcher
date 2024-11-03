import { ParseModVersions } from "./GetBeatSaberUtils";

let testData = [
  {
    name: "Nya",
    description:
      "Beat Saber mod for displaying nya-tastic images from various anime themed web APIs!",
    id: "Nya",
    version: "0.2.8",
    download:
      "https://github.com/FrozenAlex/Nya-quest/releases/download/v0.2.8-bs-1.25.1/Nya.qmod",
    source: "https://github.com/FrozenAlex/Nya-quest/",
    author: "FrozenAlex",
  },
  {
    name: "BetterSongSearch",
    description:
      "Search and download songs with a lot of filtering and sorting options and without frustration!",
    id: "BetterSongSearch",
    version: "1.0.3",
    download:
      "https://github.com/bsq-ports/BetterSongSearchQuest/releases/download/v1.0.3-bs-1.25.1/BetterSongSearch.qmod",
    source: "https://github.com/bsq-ports/BetterSongSearchQuest/",
    author: "FrozenAlex, FernTheDev, FutureMapper, and Christoffyw",
  },
  {
    name: "Nya",
    description:
      "Beat Saber mod for displaying nya-tastic images from various anime themed web APIs!",
    id: "Nya",
    version: "0.2.7",
    download:
      "https://github.com/FrozenAlex/Nya-quest/releases/download/v0.2.7-bs-1.25.1/Nya.qmod",
    source: "https://github.com/FrozenAlex/Nya-quest/",
    author: "FrozenAlex",
  },
  {
    name: "BetterSongSearch",
    description:
      "Search and download songs with a lot of filtering and sorting options and without frustration!",
    id: "BetterSongSearch",
    version: "1.0.2",
    download:
      "https://github.com/bsq-ports/BetterSongSearchQuest/releases/download/v1.0.2-bs-1.25.1/BetterSongSearch.qmod",
    source: "https://github.com/bsq-ports/BetterSongSearchQuest/",
    author: "FrozenAlex, FernTheDev, FutureMapper, and Christoffyw",
  },
];

test("ParseModVersions", () => {
  let parsed = ParseModVersions(testData);

  let Nya = parsed.find((mod) => mod.id === "Nya");
  expect(Nya).toBeDefined();
  expect(Nya?.versions.length).toBe(2);
  expect(Nya?.versions[0].version).toBe("0.2.8");
  expect(Nya?.versions[1].version).toBe("0.2.7");

  let BetterSongSearch = parsed.find((mod) => mod.id === "BetterSongSearch");
  expect(BetterSongSearch).toBeDefined();
  expect(BetterSongSearch?.versions.length).toBe(2);
  expect(BetterSongSearch?.versions[0].version).toBe("1.0.3");
  expect(BetterSongSearch?.versions[1].version).toBe("1.0.2");

  expect(parsed.length).toBe(2);
});
