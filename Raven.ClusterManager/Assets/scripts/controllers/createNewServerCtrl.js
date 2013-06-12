/// <reference path="../main.js" />
/*global angular:false */
'use strict';
clusterManagerApp.controller('CreateNewServerCtrl', function createNewServerCtrl($scope, $http) {

    $scope.saveServer = function () {
        if ($scope.Url == null || ($scope.Credendial == null && $scope.selectCredential == null) || ($scope.Credendial == "" && $scope.selectCredential == null))
            return;
        if ($scope.Url == "" || ($scope.Credendial == "" && $scope.selectCredential == "") || ($scope.Credendial == null && $scope.selectCredential == ""))
            return;
        
        
        var serverToSave = $scope.selectCredential == null // if null
            ? { url: $scope.Url, credendial: $scope.Credendial }// then
            : { url: $scope.Url, credendial: $scope.selectCredential };// else
        
        $scope.selectCredential = "";
        $scope.Url = "";
        $scope.Credendial = "";
        
        $http.post('/api/servers/createNewServer', serverToSave).success(alert('Server created!'));
        
    };
    $scope.chooseCredendial = function () {
        $http.get('/api/servers').success(function (result) {
            $scope.stats = result;
        });
    };
});