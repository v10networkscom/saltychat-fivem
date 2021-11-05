const messageType = Object.freeze({ "display": 0, "pluginState": 1, "setRange": 2, "setSoundState": 3 })
const soundState = Object.freeze({ "idle": 0, "talking": 1, "microphoneMuted": 2, "soundMuted": 3 })

let lastRangeToggle = Date.now();

$(function(){
    window.addEventListener("message", function(event) {
		switch (event.data.type) {
			case messageType.display: {
				if (event.data.data)
					$(".ui").fadeIn();
				else
					$(".ui").fadeOut();

				break;
			}
			case messageType.pluginState: {
				// ToDo

				break;
			}
			case messageType.setRange: {
				lastRangeToggle = Date.now();

				$("#range-box").html(event.data.data);
				showProximityAnimation();

				break;
			}
			case messageType.setSoundState: {
				switch (event.data.data) {
					case soundState.idle: {
						setMicrophoneMuted(false);
						setSoundMuted(false);
						$("#icon").css("text-shadow", "unset");
						$('#icon').css({'color': '#fff'});

						break;
					}
					case soundState.talking: {
						$("#icon").css("text-shadow", "0px 0px 20px #00ff0d");
						$('#icon').css({'color': '#00ff00'});

						break;
					}
					case soundState.microphoneMuted: {
						setMicrophoneMuted(true);
	
						break;
					}
					case soundState.soundMuted: {
						setSoundMuted(true);

						break;
					}
				}

				break;
			}
		}
    });
});

function setMicrophoneMuted(value) {
	if (value) {
		$('#icon').css({'color': '#a50000'});
		$("#icon").css("text-shadow", "0px 0px 20px #a50000");
		document.getElementById("icon").innerHTML = '<i class="fa fa-microphone-slash fa-fw"></i>';
	} else {
		document.getElementById("icon").innerHTML = '<i class="fa fa-microphone fa-fw"></i>';
	}
}

function setSoundMuted(value) {
	if (value) {
		$('#icon').css({'color': '#a50000'});
		$("#icon").css("text-shadow", "0px 0px 20px #a50000");
		document.getElementById("icon").innerHTML = '<i class="fa fa-volume-mute fa-fw"></i>';
	} else {
		document.getElementById("icon").innerHTML = '<i class="fa fa-microphone fa-fw"></i>';
	}
}

function showProximityAnimation() {
	$("#range-backgound").fadeIn(500);

	setTimeout(function(){
		if (Date.now() > lastRangeToggle + 2000)
			$("#range-backgound").fadeOut();
	}, 2100);
}
