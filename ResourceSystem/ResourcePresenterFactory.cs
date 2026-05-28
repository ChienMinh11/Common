using UnityEngine;

namespace ChieChie.Core
{
    public class ResourcePresenterFactory
    {
        private readonly IEventService _eventService;
        private readonly IReadOnlyInfiniteStatus _infiniteStatus;

        public ResourcePresenterFactory(IEventService eventService, IReadOnlyInfiniteStatus infiniteStatus)
        {
            _eventService = eventService;
            _infiniteStatus = infiniteStatus;
        }

        public IResourcePresenter CreatePresenter(ResourceType resourceId, IResourceView view, object model, bool useLongNumbers)
        {
            if (useLongNumbers)
            {
                if (model is ResourceModel<long> longModel)
                {
                    return new ResourcePresenter<long>(longModel, view, resourceId, new LongConverter(), _eventService, _infiniteStatus);
                }
                Debug.LogError("[ResourcePresenterFactory] Model provided is not ResourceModel<long>");
                return null;
            }
            else
            {
                if (model is ResourceModel<int> intModel)
                {
                    return new ResourcePresenter<int>(intModel, view, resourceId, new IntConverter(), _eventService, _infiniteStatus);
                }
                Debug.LogError("[ResourcePresenterFactory] Model provided is not ResourceModel<int>");
                return null;
            }
        }
    }
}