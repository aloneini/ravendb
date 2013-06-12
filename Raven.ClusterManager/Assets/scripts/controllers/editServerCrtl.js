/// <reference path="../main.js" />
/*global angular:false */
'use strict';
clusterManagerApp.controller('editServerCtrl', function editServerCtrl($scope, $http) {
    $scope.chooseServer = function () {

        $http.get('/api/servers').success(function (result) {
            $scope.stats = result;

        });
    };
    $scope.saveChanges = function () {
        var changedServer =
            {
                id: $scope.selectedServer.id,
                url: $scope.selectedServer.url,
                serverName: $scope.selectedServer.serverName,
                clusterName: $scope.selectedServer.clusterName,
                isOnline: $scope.selectedServer.isOnline,
                credentialsId: $scope.selectedServer.credentialsId
            };
        $http.put('/api/servers/editServer', changedServer).success(function () {
            alert('The changes had been saved.');
        });
    };

});