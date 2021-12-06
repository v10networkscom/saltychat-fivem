const messageType = Object.freeze({ "display": 0, "pluginState": 1, "setRange": 2, "setSoundState": 3, "setRadioChannel": 4, "setRadioState": 5, "setPosition": 6 })
const soundState = Object.freeze({ "idle": 0, "talking": 1, "microphoneMuted": 2, "soundMuted": 3 })

let lastRangeToggle = Date.now();
let lastRadioToggle = Date.now();

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
			case messageType.setRadioChannel: {
				lastRadioToggle = Date.now();

				$("#radio-box").html(event.data.data);
				showRadioChannel();

				break;
			}
			case messageType.setSoundState: {
				switch (event.data.data) {
					case soundState.idle: {
						setMicrophoneMuted(false);
						setSoundMuted(false);
						$("#icon").css("text-shadow", "unset");
						$('#icon').css({'color': '#fff'});

						$("#range-backgound").fadeOut(500);
						break;
					}
					case soundState.talking: {
						$("#range-backgound").fadeIn(500);
						$("#icon").css("text-shadow", "0px 0px 20px #00ff0d");
						$('#icon').css({'color': '#00ff00'});
						lastRangeToggle = Date.now();

						break;
					}
					case soundState.microphoneMuted: {
						$("#range-backgound").fadeOut(500);
						setMicrophoneMuted(true);
	
						break;
					}
					case soundState.soundMuted: {
						$("#range-backgound").fadeOut(500);
						setSoundMuted(true);

						break;
					}
				}

				break;
			}
			case messageType.setRadioState: {
				switch (event.data.data) {
					case soundState.idle: {
						$("#icon_radio").css("text-shadow", "unset");
						$('#icon_radio').css({'color': '#FFFFFF'});
						$("#radio-backgound").fadeOut(500);
						break;
					}
					case soundState.talking: {
						$("#radio-backgound").fadeIn(500);
						$("#icon_radio").css("text-shadow", "0px 0px 20px #00ff0d");
						$('#icon_radio').css({'color': '#00FF00'});
						lastRadioToggle = Date.now();
						break;
					}
					case soundState.microphoneMuted: {
						break;
					}
					case soundState.soundMuted: {
						$("#radio-backgound").fadeOut(500);
						$('#icon_radio').css({'color': '#FF0000'});
						break;
					}
				}

				break;
			}
			case messageType.setPosition: {
				$(".microphone").css({'top': event.data.data[0]});
				$(".microphone").css({'left': event.data.data[1]});
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

function showRadioChannel() {
	$("#radio-backgound").fadeIn(500);

	setTimeout(function(){
		if (Date.now() > lastRadioToggle + 2000)
			$("#radio-backgound").fadeOut();
	}, 2100);
}