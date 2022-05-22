public interface ILivableModification : IUniquelyIdentifiable
{
    int modificationOwnerRefId { get; set; }
}