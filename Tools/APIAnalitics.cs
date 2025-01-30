namespace FishyAPI.Tools;

public class APIAnalitics
{
    public enum TimingType
    {
        GPTime,
        GetResponseTime,
        PutResponseTime,
        PostResponseTime,
        DeleteResponseTime
    }

    public enum ResponseType
    {
        Success,
        Error,
        NotFound,
        Info,
    }

    public struct TimingStats
    {
        public UInt64 TimingPoints = 0;
        public float ResponseTimeAverage = 0.0f;
        public float ResponseTimeMaximum = 0.0f;
        public float ResponseTimeMinimum = 0.0f;

        public TimingStats()
        {
        }
    }
    
    public Dictionary<TimingType, TimingStats> APIResponseTimings = new Dictionary<TimingType, TimingStats>();
    public Dictionary<ResponseType, int> APISuccessRates = new Dictionary<ResponseType, int>();

    private APIAnalitics()
    {
        APIResponseTimings.Add(TimingType.GPTime, new TimingStats());
        APIResponseTimings.Add(TimingType.GetResponseTime, new TimingStats());
        APIResponseTimings.Add(TimingType.PutResponseTime, new TimingStats());
        APIResponseTimings.Add(TimingType.PostResponseTime, new TimingStats());
        APIResponseTimings.Add(TimingType.DeleteResponseTime, new TimingStats());
        APISuccessRates.Add(ResponseType.Success, 0);
        APISuccessRates.Add(ResponseType.Error, 0);
        APISuccessRates.Add(ResponseType.NotFound, 0);
        APISuccessRates.Add(ResponseType.Info, 0);
    }

    public void AddAnalytic(TimingType timingType, float ResponseTime, ResponseType responseType)
    {
        var apiResponseTiming = APIResponseTimings[timingType];
        apiResponseTiming.ResponseTimeAverage += (ResponseTime / apiResponseTiming.TimingPoints);
        
        if (apiResponseTiming.ResponseTimeMaximum < ResponseTime)
        {
            apiResponseTiming.ResponseTimeMaximum = ResponseTime;
        } else if (apiResponseTiming.ResponseTimeMinimum > ResponseTime) {
            apiResponseTiming.ResponseTimeMinimum = ResponseTime;
        }
        
        apiResponseTiming.TimingPoints += 1;
        
        APIResponseTimings[timingType] = apiResponseTiming;
        
        APISuccessRates[responseType] += 1;
    }
}