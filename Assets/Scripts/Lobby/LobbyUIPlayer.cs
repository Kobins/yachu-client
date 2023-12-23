using UnityEngine;
using UnityEngine.UI;

namespace Yachu.Client.Lobby {
    public class LobbyUIPlayer : MonoBehaviour {
        
        public Text _playerName;
        public Image _icon;
        public Button _button;

        public Sprite _hostSprite;
        public Sprite _readySprite;
        private static readonly Color DefaultColor = new Color(52f / 255f, 52f / 255f, 52f / 255f, 1f);
        private static readonly Color TransparentColor = new Color(52f / 255f, 52f / 255f, 52f / 255f, 0.1f);
        
        [HideInInspector] 
        private bool _host = false;

        public bool Host {
            get => _host;
            set {
                _host = value;
                UpdateIcon();
            }
        }
        private bool _ready = false;
        public bool Ready {
            get => _ready;
            set {
                _ready = value;
                UpdateIcon();
            }
        }

        public void Reset() {
            Ready = false;
            Host = false;
        }

        private void UpdateIcon() {
            if (_host) {
                _icon.sprite = _hostSprite;
                _icon.color = DefaultColor;
                return;
            }
            _icon.sprite = _readySprite;
            _icon.color = _ready ? DefaultColor : TransparentColor;
        }
    }
}