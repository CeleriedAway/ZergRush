using ZergRush.ReactiveCore;

public interface IHasUpdateEvent
{
    IEventStream Updated { get; }
}