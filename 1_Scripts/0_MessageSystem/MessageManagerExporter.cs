using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// MessageManager数据导出器
/// 导出消息系统的当前状态，包括所有注册的事件和监听器信息
/// </summary>
public class MessageManagerExporter : IDataExporter
{
    public string ExporterName => "MessageManager";
    public bool IsEnabled => true;

    public string ExportToJson()
    {
        var data = new MessageManagerData
        {
            exportTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            totalEvents = MessageManager.GetEventCount(),
            permanentEvents = MessageManager.GetPermanentEvents(),
            eventDetails = GetEventDetails()
        };

        return JsonUtility.ToJson(data, true);
    }

    private List<EventDetail> GetEventDetails()
    {
        var details = new List<EventDetail>();
        var eventTable = MessageManager.GetEventTable();

        foreach (var kvp in eventTable)
        {
            var detail = new EventDetail
            {
                eventType = kvp.Key.ToString(),
                listenerCount = MessageManager.GetListenerCount(kvp.Key),
                isPermanent = MessageManager.IsPermanent(kvp.Key)
            };
            details.Add(detail);
        }

        return details;
    }

    [System.Serializable]
    private class MessageManagerData
    {
        public string exportTime;
        public int totalEvents;
        public List<string> permanentEvents;
        public List<EventDetail> eventDetails;
    }

    [System.Serializable]
    private class EventDetail
    {
        public string eventType;
        public int listenerCount;
        public bool isPermanent;
    }
}
