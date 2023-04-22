using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using ComputerUtils.Android.Logging;

namespace QuestAppVersionSwitcher
{
    public class FileJunkDownloader
    {
        private readonly string url;
        private readonly int length;
        private readonly int start;

        public FileJunkDownloader(string url, int start, int length)
        {
            this.url = url;
            this.start = start;
            this.length = length;
        }

        public void DownloadFilePart()
        {
            
        }
    }

    public class FileDownloader
    {
            public long downloadedBytes = 0;
            public long totalBytes = 0;
            public bool error = false;
            public bool canceled = false;
            public Action OnDownloadComplete;
            public Action OnDownloadProgress;
            public Action OnDownloadError;

            public void DownloadFile(string url, string savePath, int numConnections)
            {
                Thread t = new Thread(() =>
                {
                    DownloadFileInternal(url, savePath, numConnections);
                });
                t.Start();
            }

            public void DownloadFileInternal(string url, string savePath, int numConnections)
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.AllowAutoRedirect = true;
        
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                long fileSize = response.ContentLength;
                response.Close();
                totalBytes = fileSize;
        
                Logger.Log("File size: " + fileSize);
        
                long chunkSize = fileSize / numConnections;
        
                long[] startPosArray = new long[numConnections];
                long[] endPosArray = new long[numConnections];
        
                for (int i = 0; i < numConnections; i++)
                {
                    startPosArray[i] = i * chunkSize;
                    endPosArray[i] = (i + 1) * chunkSize - 1;
        
                    if (i == numConnections - 1) endPosArray[i] = fileSize - 1;
        
                    Console.WriteLine("Connection " + i + " range: " + startPosArray[i] + "-" + endPosArray[i]);
                }
        
                // Create an array to store the downloaded bytes for each connection
                long[] bytesDownloadedArray = new long[numConnections];
        
                // Download each chunk in a separate thread
                for (int i = 0; i < numConnections; i++)
                {
                    int chunkIndex = i;
                    new Thread(() =>
                    {
                        DownloadChunk(url, savePath, startPosArray[chunkIndex], endPosArray[chunkIndex], chunkIndex, ref bytesDownloadedArray);
                    }).Start();
                }
        
                // Wait for all threads to complete
                while (true)
                {
                    downloadedBytes = 0;
                    for (int i = 0; i < numConnections; i++)
                    {
                        downloadedBytes += bytesDownloadedArray[i];
                    }

                    if (canceled || error) return;
        
                    //double progress = (double)downloadedBytes / fileSize * 100;
                    //Logger.Log("Download progress: " + progress.ToString("0.00") + "%");
                    OnDownloadProgress.Invoke();
        
                    if (downloadedBytes == fileSize) break;
                    Thread.Sleep(200);
                }
        
               Logger.Log("Download complete!");

                // Merge downloaded chunks into a single file
                using (FileStream fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write))
                {
                    byte[] buffer = new byte[4096];

                    for (int i = 0; i < numConnections; i++)
                    {
                        using (FileStream chunkStream = new FileStream(savePath + "." + i, FileMode.Open, FileAccess.Read))
                        {
                            int bytesRead;
                            while ((bytesRead = chunkStream.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                fileStream.Write(buffer, 0, bytesRead);
                            }
                        }

                        File.Delete(savePath + "." + i);
                    }
                }

                Logger.Log("File saved at " + savePath);
                OnDownloadComplete?.Invoke();
            }

            public void DownloadChunk(string url, string savePath, long startPos, long endPos, int chunkIndex, ref long[] bytesDownloadedArray)
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.AddRange(startPos, endPos);
                try
                {
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                    Stream stream = response.GetResponseStream();

                    byte[] buffer = new byte[4096];
                    int bytesRead;
                    long totalBytesRead = 0;

                    using (FileStream fileStream =
                           new FileStream(savePath + "." + chunkIndex, FileMode.Create, FileAccess.Write))
                    {
                        while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            if (error || canceled)
                            {
                                stream.Close();
                                response.Close();
                                return;
                            }
                            fileStream.Write(buffer, 0, bytesRead);
                            totalBytesRead += bytesRead;
                            bytesDownloadedArray[chunkIndex] = totalBytesRead;
                        }
                    }

                    stream.Close();
                    response.Close();

                    Logger.Log("Chunk " + chunkIndex + " download complete!");
                }
                catch (Exception e)
                {
                    Logger.Log("Error while downloading file chunk " + chunkIndex + ": " + e);
                    Cancel();
                    error = true;
                    OnDownloadError.Invoke();
                }
        
                
            }

            public void Cancel()
            {
                canceled = true;
            }
    }
}