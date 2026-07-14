using ChieChie.Constracts;
using ChieChie.Core;

namespace ChieChie.Resource
{
    public sealed class ResourcePresenterFactory
        : IPresenterFactory<ResourcePresenter, IResourceView>
    {
        private readonly ResourceModel _model;

        public ResourcePresenterFactory(ResourceModel model)
        {
            _model = model;
        }

        public ResourcePresenter Create(IResourceView view)
        {
            return new ResourcePresenter(view, _model);
        }
    }
}
