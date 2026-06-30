using System;
using System.Collections.Generic;

namespace ChieChie.GamePass
{
    public class PassModel
    {
        private readonly PassDatabase _database;
        private readonly IPassSaveAdapter _passSaveAdapter;
        private readonly PassEventScheduler _eventScheduler;
        
        public PassModel(PassDatabase database, IPassSaveAdapter passSaveAdapter, PassEventScheduler eventScheduler)
        {
            _database = database;
            _passSaveAdapter = passSaveAdapter;
            _eventScheduler = eventScheduler;
            Initialize();
        }
        
        public void Initialize()
        {
         
           
        }

        public void Cleanup()
        {
          
        }
    }
}