﻿using System.Text.Json.Serialization;

namespace MediasoupSharp.Meeting.Models
{
    public class ApiResult
    {
        public int Code { get; set; } = 200;

        public string Message { get; set; } = "Success";
    }

    public class ApiResult<T> : ApiResult
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public T? Data { get; set; }
    }
}
