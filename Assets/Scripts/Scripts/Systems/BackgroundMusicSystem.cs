using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Client {
    sealed class BackgroundMusicSystem : IEcsRunSystem, IEcsInitSystem {
        private EcsCustomInject<SceneService> _sceneService;
        private EcsCustomInject<MetaSceneService> _metaSceneService;
        private EcsCustomInject<BetweenScenesService> _betweenScenesService;
        
        private EcsPoolInject<BackgroundMusicComponent> _backgroundMusicComponentsPool;
        private EcsPoolInject<CurrentGameComponent> _currentGameComponentsPool;
        
        private EcsFilterInject<Inc<BackgroundMusicComponent>> _backgroundMusicComponentsFilter;
        private EcsFilterInject<Inc<EndRoundEvent>> _endRoundEventsFilter;

        public void Run(IEcsSystems systems) {
            foreach (var bgMusicEntity in _backgroundMusicComponentsFilter.Value) {
                ref var backgroundMusicCmp = ref _backgroundMusicComponentsPool.Value.Get(bgMusicEntity);
                foreach (var endRound in _endRoundEventsFilter.Value) {
                    int currentRound = _currentGameComponentsPool.Value.Get(bgMusicEntity).CurrentRound;
                    var needMusics = backgroundMusicCmp.IsBossMusic
                        ? _betweenScenesService.Value.GetMapConfig().MapVisualInfoConfigs[0].bossBackgroundMusic
                        : _betweenScenesService.Value.GetMapConfig().MapVisualInfoConfigs[0].defaultBackgroundMusic;
                    int randomMusicIndex = Random.Range(0, needMusics.Length);
                    if (currentRound != 0 && currentRound % 5 == 0)
                        StartNewMusic(true, randomMusicIndex);

                    else if (currentRound != 1 && currentRound % 5 == 1)
                        StartNewMusic(false, randomMusicIndex);
                }
           
                backgroundMusicCmp.CurrentMusicTimeRemaining -= Time.deltaTime;

                if (backgroundMusicCmp.CurrentMusicTimeRemaining <= 0) {
                    ChangeToNextMusic();
                }

                if (backgroundMusicCmp.NeedChangeMusic) {
                    backgroundMusicCmp.FadeTimer -= Time.deltaTime;
                    _betweenScenesService.Value.BackgroundMusicAudio.volume =
                        backgroundMusicCmp.FadeTimer * DataPersistenceManagerSystem._gameData.MusicVolume;
                    if (backgroundMusicCmp.FadeTimer <= 0)
                        SetMusic();
                } else if (backgroundMusicCmp.FadeTimer < 1) {
                    backgroundMusicCmp.FadeTimer += Time.deltaTime;
                    _betweenScenesService.Value.BackgroundMusicAudio.volume =
                        backgroundMusicCmp.FadeTimer * DataPersistenceManagerSystem._gameData.MusicVolume;
                    if (backgroundMusicCmp.FadeTimer > 1) {
                        backgroundMusicCmp.FadeTimer = 1;
                    }
                }
            }

            
        }

        public void Init(IEcsSystems systems) {
            if (_metaSceneService.Value)
                _backgroundMusicComponentsPool.Value.Add(_metaSceneService.Value.MainEntity).FadeTimer = 1;
            else
                _backgroundMusicComponentsPool.Value.Add(_sceneService.Value.PlayerEntity).FadeTimer = 1;

            var mapMusics = _betweenScenesService.Value.GetMapConfig().MapVisualInfoConfigs[0].defaultBackgroundMusic;
            var needMusic = Random.Range(0, mapMusics.Length);

            StartNewMusic(false, needMusic);
        }

        private void ChangeToNextMusic() {
            foreach (var bgMusicEntity in _backgroundMusicComponentsFilter.Value) {
                ref var backgroundMusicCmp = ref _backgroundMusicComponentsPool.Value.Get(bgMusicEntity);
                var needMusics = backgroundMusicCmp.IsBossMusic
                    ? _betweenScenesService.Value.GetMapConfig().MapVisualInfoConfigs[0].bossBackgroundMusic
                    : _betweenScenesService.Value.GetMapConfig().MapVisualInfoConfigs[0].defaultBackgroundMusic;
                backgroundMusicCmp.CurrentMusicIndex++;
                if (needMusics.Length <= backgroundMusicCmp.CurrentMusicIndex)
                    backgroundMusicCmp.CurrentMusicIndex = 0;

                StartNewMusic(backgroundMusicCmp.IsBossMusic, backgroundMusicCmp.CurrentMusicIndex);
            }
        }

        private void StartNewMusic(bool isBoss, int musicIndex) {
            foreach (var bgMusicEntity in _backgroundMusicComponentsFilter.Value) {
                ref var backgroundMusicCmp = ref _backgroundMusicComponentsPool.Value.Get(bgMusicEntity);
                backgroundMusicCmp.NeedChangeMusic = true;
                backgroundMusicCmp.IsBossMusic = isBoss;
                backgroundMusicCmp.CurrentMusicIndex = musicIndex;
                var needMusic = isBoss
                    ? _betweenScenesService.Value.GetMapConfig().MapVisualInfoConfigs[0].bossBackgroundMusic[musicIndex]
                    : _betweenScenesService.Value.GetMapConfig().MapVisualInfoConfigs[0].defaultBackgroundMusic[musicIndex];
                backgroundMusicCmp.CurrentMusicTimeRemaining = needMusic.length;
            }
        }

        private void SetMusic() {
            foreach (var bgMusicEntity in _backgroundMusicComponentsFilter.Value) {
                ref var backgroundMusicCmp = ref _backgroundMusicComponentsPool.Value.Get(bgMusicEntity);
                backgroundMusicCmp.FadeTimer = 0;
                var needMusic = backgroundMusicCmp.IsBossMusic
                    ? _betweenScenesService.Value.GetMapConfig().MapVisualInfoConfigs[0]
                        .bossBackgroundMusic[backgroundMusicCmp.CurrentMusicIndex]
                    : _betweenScenesService.Value.GetMapConfig().MapVisualInfoConfigs[0]
                        .defaultBackgroundMusic[backgroundMusicCmp.CurrentMusicIndex];

                _betweenScenesService.Value.BackgroundMusicAudio.clip = needMusic;
                _betweenScenesService.Value.BackgroundMusicAudio.Play();
                backgroundMusicCmp.NeedChangeMusic = false;
            }
        }
    }
}