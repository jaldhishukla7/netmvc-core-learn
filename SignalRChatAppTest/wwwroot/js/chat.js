<script>
    "use strict";

    var connection = new signalR.HubConnectionBuilder().withUrl("/chatHub").build();
    var typingTimer;
    var isTyping = false;
    var selectedUsers = [];

    // Start the connection
    connection.start().then(function () {
        console.log("SignalR Connected.");
}).catch(function (err) {
        console.error(err.toString());
});

    // Connection events
    connection.on("ReceiveMessage", function (message) {
        addMessageToChat(message);
});

    connection.on("UserTyping", function (data) {
        showTypingIndicator(data);
});

    connection.on("UserOnline", function (userId) {
        updateUserStatus(userId, true);
});

    connection.on("UserOffline", function (userId) {
        updateUserStatus(userId, false);
});

    // Chat functions
    function sendMessage() {
    var messageInput = document.getElementById("messageInput");
    var message = messageInput.value.trim();

    if (message && typeof chatRoomId !== 'undefined') {
        connection.invoke("SendMessage", chatRoomId, message).catch(function (err) {
            console.error(err.toString());
        });
    messageInput.value = "";
    stopTyping();
    }
}

    function handleKeyPress(event) {
    if (event.key === "Enter") {
        event.preventDefault();
    sendMessage();
    return;
    }

    // Handle typing indicator
    if (typeof chatRoomId !== 'undefined') {
        if (!isTyping) {
        isTyping = true;
    connection.invoke("TypingIndicator", chatRoomId, true);
        }

    clearTimeout(typingTimer);
    typingTimer = setTimeout(function() {
        stopTyping();
        }, 2000);
    }
}

    function stopTyping() {
    if (isTyping && typeof chatRoomId !== 'undefined') {
        isTyping = false;
    connection.invoke("TypingIndicator", chatRoomId, false);
    }
}

    function addMessageToChat(message) {
    var messagesContainer = document.getElementById("messagesContainer");
    if (!messagesContainer) return;

    var messageDiv = document.createElement("div");
    messageDiv.className = "message " + (message.SenderId === currentUserId ? "sent" : "received");

    var contentDiv = document.createElement("div");
    contentDiv.className = "message-content";

    if (message.SenderId !== currentUserId) {
        var senderDiv = document.createElement("div");
    senderDiv.className = "message-sender";
    senderDiv.textContent = message.SenderName;
    contentDiv.appendChild(senderDiv);
    }

    var textDiv = document.createElement("div");
    textDiv.className = "message-text";
    textDiv.textContent = message.Content;
    contentDiv.appendChild(textDiv);

    var timeDiv = document.createElement("div");
    timeDiv.className = "message-time";
    var sentTime = new Date(message.SentAt);
    timeDiv.textContent = sentTime.toLocaleTimeString([], {hour: '2-digit', minute:'2-digit'});
    contentDiv.appendChild(timeDiv);

    messageDiv.appendChild(contentDiv);
    messagesContainer.appendChild(messageDiv);
    messagesContainer.scrollTop = messagesContainer.scrollHeight;

    // Hide typing indicator
    hideTypingIndicator();
}

    function showTypingIndicator(data) {
    if (data.UserId === currentUserId) return;

    var indicator = document.getElementById("typingIndicator");
    var text = document.getElementById("typingText");

    if (indicator && text) {
        if (data.IsTyping) {
        text.textContent = data.UserName + " is typing...";
    indicator.style.display = "block";
        } else {
        indicator.style.display = "none";
        }
    }
}

    function hideTypingIndicator() {
    var indicator = document.getElementById("typingIndicator");
    if (indicator) {
        indicator.style.display = "none";
    }
}

    function updateUserStatus(userId, isOnline) {
    var statusElements = document.querySelectorAll('[data-user-id="' + userId + '"] .online-status');
    statusElements.forEach(function(element) {
        element.className = "online-status " + (isOnline ? "online" : "offline");
    });
}

    function openChat(chatRoomId) {
        window.location.href = '/Chat/Room/' + chatRoomId;
}

    // New Chat Modal functions
    function toggleChatType() {
    var chatType = document.getElementById("chatType").value;
    var groupNameDiv = document.getElementById("groupNameDiv");

    if (chatType === "group") {
        groupNameDiv.style.display = "block";
    } else {
        groupNameDiv.style.display = "none";
    }
}

    function loadUsers() {
        fetch('/Chat/GetUsers')
            .then(response => response.json())
            .then(users => {
                var usersList = document.getElementById("usersList");
                usersList.innerHTML = "";

                users.forEach(function (user) {
                    var userItem = document.createElement("div");
                    userItem.className = "user-item";
                    userItem.onclick = function () { toggleUserSelection(user.Id, userItem); };

                    var checkbox = document.createElement("input");
                    checkbox.type = "checkbox";
                    checkbox.id = "user_" + user.Id;

                    var userInfo = document.createElement("div");
                    userInfo.className = "user-info";

                    var userName = document.createElement("div");
                    userName.className = "user-name";
                    userName.textContent = user.Name;

                    var userEmail = document.createElement("div");
                    userEmail.className = "user-email";
                    userEmail.textContent = user.Email;

                    var statusSpan = document.createElement("span");
                    statusSpan.className = "online-status " + (user.IsOnline ? "online" : "offline");
                    statusSpan.style.position = "relative";
                    statusSpan.style.right = "auto";
                    statusSpan.style.top = "auto";
                    statusSpan.style.transform = "none";
                    statusSpan.style.marginLeft = "10px";

                    userInfo.appendChild(userName);
                    userInfo.appendChild(userEmail);

                    userItem.appendChild(checkbox);
                    userItem.appendChild(userInfo);
                    userItem.appendChild(statusSpan);

                    usersList.appendChild(userItem);
                });
            })
            .catch(error => {
                console.error('Error loading users:', error);
            });
}

    function toggleUserSelection(userId, element) {
    var checkbox = element.querySelector('input[type="checkbox"]');
    checkbox.checked = !checkbox.checked;

    if (checkbox.checked) {
        element.classList.add("selected");
    if (!selectedUsers.includes(userId)) {
        selectedUsers.push(userId);
        }
    } else {
        element.classList.remove("selected");
        selectedUsers = selectedUsers.filter(id => id !== userId);
    }

    // For private chat, allow only one selection
    var chatType = document.getElementById("chatType").value;
    if (chatType === "private" && selectedUsers.length > 1) {
        // Unselect previous selections
        var otherCheckboxes = document.querySelectorAll('.user-item input[type="checkbox"]:not(#user_' + userId + ')');
    otherCheckboxes.forEach(function(cb) {
        cb.checked = false;
    cb.closest('.user-item').classList.remove('selected');
        });
    selectedUsers = [userId];
    }
}

    function createChat() {
    if (selectedUsers.length === 0) {
        alert("Please select at least one participant.");
    return;
    }

    var chatType = document.getElementById("chatType").value;
    var groupName = document.getElementById("groupName").value;

    var requestData = {
        Name: chatType === "group" ? groupName : null,
    ParticipantIds: selectedUsers
    };

    fetch('/Chat/CreateOrGetChat', {
        method: 'POST',
    headers: {
        'Content-Type': 'application/json',
        },
    body: JSON.stringify(requestData)
    })
    .then(response => response.json())
    .then(data => {
        if (data.chatRoomId) {
        window.location.href = '/Chat/Room/' + data.chatRoomId;
        }
    })
    .catch(error => {
        console.error('Error creating chat:', error);
    });
}

    // Initialize modal events
    document.addEventListener('DOMContentLoaded', function() {
    var newChatModal = document.getElementById('newChatModal');
    if (newChatModal) {
        newChatModal.addEventListener('show.bs.modal', function () {
            selectedUsers = [];
            loadUsers();
        });

    newChatModal.addEventListener('hidden.bs.modal', function () {
        selectedUsers = [];
    document.getElementById("chatType").value = "private";
    document.getElementById("groupNameDiv").style.display = "none";
    document.getElementById("groupName").value = "";
        });
    }

    // Auto-scroll to bottom of messages
    var messagesContainer = document.getElementById("messagesContainer");
    if (messagesContainer) {
        messagesContainer.scrollTop = messagesContainer.scrollHeight;
    }

    // Join current chat room if on room page
    if (typeof chatRoomId !== 'undefined') {
        connection.invoke("JoinChatRoom", chatRoomId);
    }
});
</script>
