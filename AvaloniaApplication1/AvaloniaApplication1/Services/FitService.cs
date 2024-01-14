using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Dynastream.Fit;

namespace AvaloniaApplication1.Services;


public record Messages
{
    public List<RecordMesg?> Records { get; } = [];
    public List<SessionMesg?> Sessions { get; } = [];
}

public class FitService
{
    private readonly Messages messages = new();

    public async Task<Messages?> ParseFile(IStorageFile fitFile)
    {
        Decode decoder = new();
        MesgBroadcaster broadcaster = new();

        await using var stream = await fitFile.OpenReadAsync();

        // register broadcaster
        decoder.MesgEvent += broadcaster.OnMesg;
        decoder.MesgDefinitionEvent += broadcaster.OnMesgDefinition;

        // register boradcaster events
        broadcaster.RecordMesgEvent += BroadcasterOnRecordMesgEvent;
        broadcaster.SessionMesgEvent += BroadcasterOnSessionMesgEvent;


        bool isFitFile = decoder.IsFIT(stream);
        if (!isFitFile)
        {
            Console.WriteLine("is not fit file");
            return null;
        }

        // decode
        decoder.Read(stream);
        stream.Close();

        return messages;
    }

    private void BroadcasterOnSessionMesgEvent(object sender, MesgEventArgs e)
    {
        messages.Sessions.Add(e.mesg as SessionMesg);
    }

    private void BroadcasterOnRecordMesgEvent(object sender, MesgEventArgs e)
    {
        messages.Records.Add(e.mesg as RecordMesg);
    }
}