using System.Collections.Generic;
using UnityEngine;

namespace ChieChie.Profile
{
    [CreateAssetMenu(fileName = "FrameConfig", menuName = "CORE/Profile/Frame/FrameConfig")]
    public class FrameConfig : ScriptableObject
    {
        [SerializeField] private List<FrameData> frames = new List<FrameData>();
        [SerializeField] private FrameData defaultFrame;

        public List<FrameData> Frames => frames;

        public FrameData DefaultFrame
        {
            get => defaultFrame;
            set => defaultFrame = value;
        }

        public FrameData GetFrameById(int id)
        {
            return frames.Find(frame => frame.Id == id);
        }

        public Dictionary<int, FrameModel> GetDefaultFrameInfoDictionary()
        {
            var result = new Dictionary<int, FrameModel>();
            foreach (var frame in frames)
            {
                FrameModel frameModel = frame.ToFrameInfo();
                result[frameModel.Id] = frameModel;
            }
            return result;
        }
    }
}