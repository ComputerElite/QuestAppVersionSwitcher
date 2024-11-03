namespace QuestAppVersionSwitcher
{
    public class QAVSWebsocketMessage<T>
    {
        public string route { get; set; } = "";
        public T data { get; set; } = default(T);
        
        public QAVSWebsocketMessage(string route, T data)
        {
            this.route = route;
            this.data = data;
        }
    }
}