﻿using Newtonsoft.Json;
using ProtoBuf;

namespace playerlist.configuration;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class Config {
    public int[]? Thresholds { get; set; } = { 65, 125, 500 };

    public string? Header { get; set; }

    public string? Footer { get; set; }

    [JsonIgnore]
    public int? MaxPlayers { get; set; }
}
