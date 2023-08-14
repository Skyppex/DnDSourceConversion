using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.EventEmitters;

namespace DnDSourceConversion;

public class QuoteSurroundingEventEmitter : ChainedEventEmitter
{
    public QuoteSurroundingEventEmitter(IEventEmitter nextEmitter)  : base(nextEmitter)
    { }

    public override void Emit(ScalarEventInfo eventInfo, IEmitter emitter)
    {
        if(eventInfo.Source.ScalarStyle == ScalarStyle.SingleQuoted)
            eventInfo.Style = ScalarStyle.DoubleQuoted;
        
        base.Emit(eventInfo, emitter);
    }
}