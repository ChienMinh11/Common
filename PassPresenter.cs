using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChieChie.GamePass
{
    public class PassPresenter
    {
        private readonly PassModel _model;
        private readonly List<IPassView> _activeViews = new List<IPassView>();
        public PassPresenter(PassModel model)
        {
           _model = model;
           Initialize();
        }

        public void Initialize()
        {
         
           
        }
        
        public void RegisterView(IPassView view)
        {
            CleanUpDestroyedViews();
            if (!_activeViews.Contains(view))
            {
                _activeViews.Add(view);
            }
        }

        public void UnregisterView(IPassView view)
        {
            if (_activeViews.Contains(view))
            {
                _activeViews.Remove(view);
            }
        }
        private void CleanUpDestroyedViews()
        {
            _activeViews.RemoveAll(view => 
                view == null || (view is MonoBehaviour mb && mb == null)
            );
        }

        public void Cleanup()
        {
          
        }

    }
}